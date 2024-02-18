using System.Buffers.Binary;
using System.Data;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using AsyncKeyedLock;
using Eum.Spotify;
using Google.Protobuf;
using Spotify.Metadata;
using Wavee.Spotify.Models.Common;

namespace Wavee.Spotify.Auth.Tcp;

public sealed class TcpAuth : IDisposable
{
    private readonly string _deviceId;
    private LoginCredentials _authenticationCredentials;

    private readonly ManualResetEvent _waitForAuth = new(false);
    private TcpClient? _client;
    private ReadOnlyMemory<byte> _receiveKey;
    private ReadOnlyMemory<byte> _sendKey;
    private string? _countryCode;
    private TaskCompletionSource<string> _countryCodeTask = new();

    private readonly AsyncNonKeyedLocker _locksLocker = new();
    private readonly Dictionary<Guid, SpotifyPackageCallback> _callbacks = new();
    private readonly Dictionary<uint, TaskCompletionSource<byte[]?>> _audioKeysCallbacks = new();

    private int _sendSeq = 1;

    private AsyncNonKeyedLocker _audioKeySeqLock = new();
    private uint _audioKeySeq = 0;

    public TcpAuth(LoginCredentials authenticationCredentials, string deviceId)
    {
        _authenticationCredentials = authenticationCredentials;
        _deviceId = deviceId;

        Task.Run(() =>
        {
            int readSeq = 1;
            while (true)
            {
                _waitForAuth.WaitOne();

                try
                {
                    if (_client is not { Connected: true })
                    {
                        _countryCodeTask.TrySetException(new InvalidOperationException("Client is not connected."));
                        readSeq = 1;
                        _waitForAuth.Reset();
                        Task.Run(async () => await Reconnect());
                        continue;
                    }


                    var type = Connection.Receive(_client!, _receiveKey, readSeq, out var payload);

                    switch (type)
                    {
                        case SpotifyPacketType.PongAck:
                            Console.WriteLine("Received pong ack");
                            break;
                        case SpotifyPacketType.Ping:
                            Console.WriteLine("Received ping, sending pong");
                            SendAndForget(SpotifyPacketType.Pong, new byte[4]);
                            break;
                        case SpotifyPacketType.CountryCode:
                            _countryCode = Encoding.ASCII.GetString(payload.ToArray());
                            _countryCodeTask.TrySetResult(_countryCode);
                            break;
                        case SpotifyPacketType.AesKeyError:
                        case SpotifyPacketType.AesKey:
                        {
                            foreach (var callback in _callbacks.ToList())
                            {
                                callback.Value(type, payload);
                            }
                            break;
                        }
                        default:
                            Console.WriteLine($"Unexpected packet type received: {type}");
                            break;
                    }

                    readSeq++;
                }
                catch (Exception e)
                {
                    _countryCodeTask.TrySetException(e);
                    Console.WriteLine(e);
                    if (e is SocketException or IOException)
                    {
                        readSeq = 1;
                        _waitForAuth.Reset();
                        Task.Run(async () => await Reconnect());
                    }
                }
            }
        });
    }

    private Guid Send(SpotifyPacketType pong, Span<byte> packet, SpotifyPackageCallback callback)
    {
        using (_locksLocker.Lock())
        {
            var callbackId = RegisterCallback(callback);
            var seq = _sendSeq;
            _sendSeq++;
            Connection.Send(_client!, _sendKey, seq, pong, packet);
            return callbackId;
        }
    }

    private void SendAndForget(SpotifyPacketType pong, Span<byte> packet)
    {
        using (_locksLocker.Lock())
        {
            var seq = _sendSeq;
            _sendSeq++;
            Connection.Send(_client!, _sendKey, seq, pong, packet);
        }
    }

    internal void ClearCallback(Guid callbackId)
    {
        using (_locksLocker.Lock())
        {
            _callbacks.Remove(callbackId);
        }
    }

    public ValueTask<string> CountryCode => _countryCodeTask.Task.IsCompletedSuccessfully
        ? new ValueTask<string>(_countryCode!)
        : new ValueTask<string>(_countryCodeTask.Task);

    private Guid RegisterCallback(SpotifyPackageCallback callback)
    {
        var guid = Guid.NewGuid();
        _callbacks.Add(guid, callback);
        return guid;
    }

    private async Task Reconnect()
    {
        _client?.Dispose();

        while (true)
        {
            try
            {
                await Authenticate();
                break;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await Task.Delay(1000);
            }
        }
    }

    public bool Healthy => _client is { Connected: true } && !_receiveKey.IsEmpty && !_sendKey.IsEmpty;

    public async Task<byte[]?> GetAudioKey(SpotifyId id, ByteString fileId, CancellationToken cancellationToken)
    {
        var (seq, callbackId) = SendAudioKeyRequest(id, fileId);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(5000);
        try
        {
            var tcs = _audioKeysCallbacks[seq];
            var result = await tcs.Task.WaitAsync(cts.Token);
            return result;
        }
        finally
        {
            _audioKeysCallbacks.Remove(seq);
            ClearCallback(callbackId);
        }
    }

    private (uint, Guid) SendAudioKeyRequest(SpotifyId id, ByteString fileId)
    {
        uint GetNextSeq()
        {
            using var locker = _audioKeySeqLock.Lock();
            var seq = _audioKeySeq;
            _audioKeySeq++;
            return seq;
        }

        var raw = id.ToRaw();
        Span<byte> data = stackalloc byte[raw.Length + fileId.Length + 2 + sizeof(uint)];

        fileId.Span.CopyTo(data);
        raw.CopyTo(data.Slice(fileId.Length));

        var seq = GetNextSeq();
        BinaryPrimitives.WriteUInt32BigEndian(data.Slice(fileId.Length + raw.Length), seq);
        BinaryPrimitives.WriteUInt16BigEndian(data.Slice(fileId.Length + raw.Length + sizeof(uint)), 0x0000);

        var tcs = new TaskCompletionSource<byte[]>();
        _audioKeysCallbacks.Add(seq, tcs);
        var callbackIdMaybe = Send(SpotifyPacketType.RequestKey, data, (x, y) => AudioKeyCallback(x, y, seq));
        return (seq, callbackIdMaybe);
    }

    private void AudioKeyCallback(SpotifyPacketType packagetype, ReadOnlySpan<byte> payload, uint checkAgainst)
    {
        if (packagetype is SpotifyPacketType.AesKey or SpotifyPacketType.AesKeyError)
        {
            var incomingSeq = BinaryPrimitives.ReadUInt32BigEndian(payload.Slice(0, 4));
            if (incomingSeq != checkAgainst)
            {
                return;
            }

            var tcs = _audioKeysCallbacks[checkAgainst];
            if (packagetype is SpotifyPacketType.AesKeyError)
            {
                tcs.TrySetResult(Array.Empty<byte>());
            }
            else
            {
                tcs.TrySetResult(payload.Slice(4, 16).ToArray());
            }
        }
    }

    public async Task<LoginCredentials> Authenticate()
    {
        const string host = "ap-gae2.spotify.com";
        const int port = 4070;
        var client = new TcpClient();
        await client.ConnectAsync(host, port);

        var (receiveKey, sendKey) = Handshake(client);
        var welcome = Authenticate(client, receiveKey, sendKey);

        var creds = new LoginCredentials
        {
            Username = welcome.CanonicalUsername,
            Typ = welcome.ReusableAuthCredentialsType,
            AuthData = welcome.ReusableAuthCredentials
        };

        _client = client;
        _receiveKey = receiveKey;
        _sendKey = sendKey;
        _authenticationCredentials = creds;
        using (_locksLocker.Lock())
        {
            _waitForAuth.Set();
            _countryCodeTask = new();
            _sendSeq = 1;
        }

        return creds;
    }

    private APWelcome Authenticate(TcpClient client, ReadOnlyMemory<byte> receiveKey, ReadOnlyMemory<byte> sendKey)
    {
        var arch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
        var cpu = arch switch
        {
            "blackfin" => CpuFamily.CpuBlackfin,
            "arm" or "arm64" => CpuFamily.CpuArm,
            "ia64" => CpuFamily.CpuIa64,
            "mips" => CpuFamily.CpuMips,
            "ppc" => CpuFamily.CpuPpc,
            "ppc64" => CpuFamily.CpuPpc64,
            "sh" => CpuFamily.CpuSh,
            "x86" => CpuFamily.CpuX86,
            "x86_64" or "AMD64" => CpuFamily.CpuX8664,
            _ => CpuFamily.CpuUnknown
        };

        var osString = Environment.OSVersion.Platform.ToString().ToLower();
        var os = osString switch
        {
            "android" => Os.Android,
            "freebsd" or "netbsd" or "openbsd" => Os.Freebsd,
            "io" => Os.Iphone,
            "linux" => Os.Linux,
            "macoS" => Os.Osx,
            "windows" => Os.Windows,
            _ => Os.Unknown
        };

        Span<byte> packet = new ClientResponseEncrypted
        {
            LoginCredentials = _authenticationCredentials,
            SystemInfo = new SystemInfo
            {
                CpuFamily = cpu,
                Os = os,
                SystemInformationString = "librespot 0.5.0-dev a211ff9",
                DeviceId = _deviceId
            },
            VersionString = "librespot 0.5.0-dev a211ff9",
        }.ToByteArray();

        Connection.Send(client, sendKey, 0, SpotifyPacketType.Login, packet);
        var packageType = Connection.Receive(client, receiveKey, 0, out var payload);

        if (packageType is SpotifyPacketType.AuthFailure)
        {
            var fail = APLoginFailed.Parser.ParseFrom(payload);
            throw new AuthException(fail.ErrorCode.ToString());
        }

        if (packageType is not SpotifyPacketType.APWelcome)
        {
            throw new AuthException("Unexpected packet type received.");
        }

        var welcome = APWelcome.Parser.ParseFrom(payload);
        return welcome;
    }

    private static (ReadOnlyMemory<byte> ReceiveKey, ReadOnlyMemory<byte> SendKey) Handshake(TcpClient tcpClient)
    {
        Span<byte> private_key_bytes = stackalloc byte[95 * 8];
        RandomNumberGenerator.Fill(private_key_bytes);

        var privateKey = new BigInteger(private_key_bytes, true, true);
        var publicKey = BigInteger.ModPow(
            DH_GENERATOR,
            privateKey,
            DH_PRIME);

        var stream = tcpClient.GetStream();
        Span<byte> nonce = stackalloc byte[0x10];
        RandomNumberGenerator.Fill(nonce);

        var clientHello = NewClientHello(publicKey, nonce);

        //Reduce number of copy operations by writing the header and payload in one go
        Span<byte> clientHelloSend = stackalloc byte[2 + sizeof(uint) + clientHello.Length];
        clientHelloSend[0] = 0;
        clientHelloSend[1] = 4;

        BinaryPrimitives.WriteUInt32BigEndian(clientHelloSend[2..], (uint)(2 + sizeof(uint) + clientHello.Length));
        clientHello.CopyTo(clientHelloSend.Slice(2 + sizeof(uint), clientHello.Length));

        stream.Write(clientHelloSend);

        Span<byte> header = stackalloc byte[4];
        stream.ReadExactly(header);
        var size = BinaryPrimitives.ReadUInt32BigEndian(header);

        Span<byte> receiveApMessage = stackalloc byte[(int)size - 4];
        stream.ReadExactly(receiveApMessage);
        var apResponse = APResponseMessage.Parser.ParseFrom(receiveApMessage);

        // Prevent man-in-the-middle attacks: check server signature
        var n = new BigInteger(Constants.SERVER_KEY, true, true)
            .ToByteArray(true, true);
        var e = new BigInteger(65537).ToByteArray(true, true);

        using var rsa = new RSACryptoServiceProvider();
        var rsaKeyInfo = new RSAParameters
        {
            Modulus = n,
            Exponent = e
        };
        rsa.ImportParameters(rsaKeyInfo);

        if (!rsa.VerifyData(apResponse
                    .Challenge
                    .LoginCryptoChallenge
                    .DiffieHellman
                    .Gs.ToByteArray(),
                apResponse.Challenge
                    .LoginCryptoChallenge
                    .DiffieHellman
                    .GsSignature
                    .ToByteArray(),
                HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1))
        {
            throw new InvalidConstraintException();
        }


        var sharedSecret = BigInteger.ModPow(
            new BigInteger(apResponse.Challenge
                .LoginCryptoChallenge
                .DiffieHellman
                .Gs.ToByteArray(), true, true),
            privateKey,
            DH_PRIME);

        //the accumulator is a collection of:
        //1) full client hello packet
        //2) apresponse header
        //3) apresponse body
        var x = compute_keys(sharedSecret,
            clientHelloSend,
            header,
            receiveApMessage, out var challenge);


        ReadOnlySpan<byte> packet = new ClientResponsePlaintext
        {
            LoginCryptoResponse = new LoginCryptoResponseUnion
            {
                DiffieHellman = new LoginCryptoDiffieHellmanResponse
                {
                    Hmac = ByteString.CopyFrom(challenge)
                }
            },
            PowResponse = new PoWResponseUnion(),
            CryptoResponse = new CryptoResponseUnion()
        }.ToByteArray();

        Span<byte> buffer = stackalloc byte[4 + packet.Length];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)(4 + packet.Length));
        packet.CopyTo(buffer[4..]);

        stream.Write(buffer);

        stream.ReadTimeout = Timeout.Infinite;

        return x;
    }

    private static (ReadOnlyMemory<byte> ReceiveKey, ReadOnlyMemory<byte> SendKey) compute_keys(
        BigInteger sharedKey,
        ReadOnlySpan<byte> clientHello,
        Span<byte> apResponseHeader,
        Span<byte> apResponseBody,
        out ReadOnlySpan<byte> challenge)
    {
        // Solve challenge
        var sharedKeyBytes = sharedKey.ToByteArray(true, true);

        Span<byte> data = stackalloc byte[0x64];

        //combine packets

        Span<byte> packets = stackalloc byte[clientHello.Length + apResponseHeader.Length + apResponseBody.Length];
        clientHello.CopyTo(packets);
        apResponseHeader.CopyTo(packets[clientHello.Length..]);
        apResponseBody.CopyTo(packets[(clientHello.Length + apResponseHeader.Length)..]);
        var packetsArray = packets.ToArray();

        for (int i = 1; i <= 5; i++)
        {
            using var hmac = new HMACSHA1(sharedKeyBytes);
            hmac.TransformBlock(packetsArray, 0, packets.Length, null, 0);
            hmac.TransformFinalBlock(new byte[] { (byte)i }, 0, 1);
            hmac.Hash.CopyTo(data[((i - 1) * 20)..]);
        }


        using var hmacFinal = new HMACSHA1(data.Slice(0, 0x14).ToArray());
        challenge = hmacFinal.ComputeHash(packets.ToArray());
        ReadOnlyMemory<byte> sendKey = data.Slice(0x14, 0x20).ToArray();
        ReadOnlyMemory<byte> recvKey = data.Slice(0x34, 0x20).ToArray();


        return (
            recvKey,
            sendKey);
    }

    private static ReadOnlySpan<byte> NewClientHello(BigInteger publicKey,
        ReadOnlySpan<byte> nonce)
    {
        var productFlag = ProductFlags.ProductFlagNone;
#if DEBUG
        productFlag = ProductFlags.ProductFlagDevBuild;
#endif
        return new ClientHello
            {
                BuildInfo = new BuildInfo
                {
                    Product = Product.Client,
                    ProductFlags = { productFlag },
                    Platform = Platform.Win32X86,
                    Version = Constants.SPOTIFY_DESKTOP_VERSION
                },
                CryptosuitesSupported =
                {
                    Cryptosuite.Shannon
                },
                ClientNonce = ByteString.CopyFrom(nonce),
                LoginCryptoHello = new LoginCryptoHelloUnion
                {
                    DiffieHellman = new LoginCryptoDiffieHellmanHello
                    {
                        Gc = ByteString.CopyFrom(publicKey.ToByteArray(true, true)),
                        ServerKeysKnown = 1
                    }
                },
                Padding = ByteString.CopyFrom(0x1e)
            }
            .ToByteArray();
    }

    private static readonly BigInteger DH_GENERATOR = new(new byte[]
    {
        0x02
    }, true, true);

    private static readonly BigInteger DH_PRIME = new(new byte[]
    {
        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xc9, 0x0f, 0xda, 0xa2, 0x21, 0x68, 0xc2,
        0x34, 0xc4, 0xc6, 0x62, 0x8b, 0x80, 0xdc, 0x1c, 0xd1, 0x29, 0x02, 0x4e, 0x08, 0x8a, 0x67,
        0xcc, 0x74, 0x02, 0x0b, 0xbe, 0xa6, 0x3b, 0x13, 0x9b, 0x22, 0x51, 0x4a, 0x08, 0x79, 0x8e,
        0x34, 0x04, 0xdd, 0xef, 0x95, 0x19, 0xb3, 0xcd, 0x3a, 0x43, 0x1b, 0x30, 0x2b, 0x0a, 0x6d,
        0xf2, 0x5f, 0x14, 0x37, 0x4f, 0xe1, 0x35, 0x6d, 0x6d, 0x51, 0xc2, 0x45, 0xe4, 0x85, 0xb5,
        0x76, 0x62, 0x5e, 0x7e, 0xc6, 0xf4, 0x4c, 0x42, 0xe9, 0xa6, 0x3a, 0x36, 0x20, 0xff, 0xff,
        0xff, 0xff, 0xff, 0xff, 0xff, 0xff
    }, true, true);


    public void Dispose()
    {
        // TODO release managed resources here
    }
}

internal delegate void SpotifyPackageCallback(SpotifyPacketType packageType, ReadOnlySpan<byte> payload);