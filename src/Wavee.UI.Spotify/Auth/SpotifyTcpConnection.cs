using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Eum.Spotify;
using Google.Protobuf;
using Microsoft.VisualBasic;
using Nito.AsyncEx;
using Wavee.UI.Spotify.Common;
using Wavee.UI.Spotify.Exceptions;
using Wavee.UI.Spotify.Interfaces;

namespace Wavee.UI.Spotify.Auth;

internal sealed class SpotifyTcpConnection : ISpotifyConnection
{
    private record WaitingForAudioKey(uint Sequence, TaskCompletionSource<byte[]> TaskCompletionSource);

    private readonly TcpClient _client;
    private readonly ReadOnlyMemory<byte> _receiveKey;
    private readonly ReadOnlyMemory<byte> _sendKey;
    private readonly List<WaitingForAudioKey> _waitingForAudioKeys = new();

    private int _sendNonce;
    private int _receiveNonce;
    private uint _audioKeyCounter = 0;

    private readonly SemaphoreSlim _sendLock = new(1, 1);

    private SpotifyTcpConnection(LoginCredentials authenticatedCredentials,
        TcpClient client,
        ReadOnlyMemory<byte> receiveKey,
        ReadOnlyMemory<byte> sendKey)
    {
        AuthenticatedCredentials = authenticatedCredentials;
        _client = client;
        _receiveKey = receiveKey;
        _sendKey = sendKey;
        _sendNonce = 1;
        _receiveNonce = 1;

        Task.Factory.StartNew(() =>
        {
            while (_client.Connected)
            {
                try
                {
                    var packetType = Receive(out var packet);
                    switch (packetType)
                    {
                        case SpotifyPacketType.Ping:
                            var pong = new byte[4];
                            Send(SpotifyPacketType.Pong, pong);
                            break;
                        case SpotifyPacketType.PongAck:
                            Console.WriteLine("Pong ack");
                            break;
                        case SpotifyPacketType.CountryCode:
                            var countryCode = Encoding.UTF8.GetString(packet);
                            Console.WriteLine($"Country code: {countryCode}");
                            break;
                        case SpotifyPacketType.AesKey:
                        case SpotifyPacketType.AesKeyError:
                        {
                            var seq = BinaryPrimitives.ReadUInt32BigEndian(packet[..4]);
                            foreach (var waiting in _waitingForAudioKeys
                                         .Where(waiting => waiting.Sequence == seq)
                                         .ToList())
                            {
                                if (packetType == SpotifyPacketType.AesKey)
                                {
                                    var key = new byte[16];
                                    packet.Slice(4, key.Length).CopyTo(key);
                                    waiting.TaskCompletionSource.SetResult(key);
                                }
                                else
                                {
                                    var errorCode = packet[4];
                                    var errorType = packet[5];
                                    waiting.TaskCompletionSource.SetException(new SpotifyException(
                                        SpotifyFailureReason.AudioKeyError,
                                        $"Error code: {errorCode}, Error type: {errorType}"));
                                }

                                _waitingForAudioKeys.Remove(waiting);
                                break;
                            }
                            break;
                        }
                        default:
                            break;
                    }

                    Console.WriteLine($"Received packet of type {packetType}");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Dispose();
                    break;
                }
            }
        }, TaskCreationOptions.LongRunning);
    }

    public LoginCredentials AuthenticatedCredentials { get; }

    public Task<byte[]> GetAudioKey(RegularSpotifyId itemId, string fileId, CancellationToken cancellationToken)
    {
        var raw = itemId.ToRaw();
        //fileId is base64 encoded
        var fileMemory = ByteString.FromBase64(fileId).Span;
        Span<byte> data = new byte[raw.Length + fileMemory.Length + 2 + sizeof(uint)];
        fileMemory.CopyTo(data);
        raw.CopyTo(data.Slice(fileMemory.Length));
        BinaryPrimitives.WriteUInt32BigEndian(data.Slice(fileMemory.Length + raw.Length), _audioKeyCounter);
        BinaryPrimitives.WriteUInt16BigEndian(data.Slice(fileMemory.Length + raw.Length + sizeof(uint)), 0x0000);
        var tcs = new TaskCompletionSource<byte[]>();
        _waitingForAudioKeys.Add(new WaitingForAudioKey(_audioKeyCounter, tcs));
        _audioKeyCounter++;
        Send(SpotifyPacketType.RequestKey, data);
        return tcs.Task;
    }

    public bool IsConnected => _client.Connected;

    public void Send(SpotifyPacketType packetType, Span<byte> packet)
    {
        _sendLock.Wait();
        try
        {
            Send(_client, _sendKey, _sendNonce, packetType, packet);
            _sendNonce++;
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private SpotifyPacketType Receive(out ReadOnlySpan<byte> packet)
    {
        var type = Receive(_client, _receiveKey, _receiveNonce, out packet);
        _receiveNonce++;
        return type;
    }

    public static async Task<ISpotifyConnection> Create(LoginCredentials loginCredentials,
        string deviceId,
        CancellationToken cancellationToken)
    {
        const string host = "ap-gae2.spotify.com";
        const int port = 4070;
        var client = new TcpClient();
        await client.ConnectAsync(host, port, cancellationToken);

        var (receiveKey, sendKey) = Handshake(client);
        var welcome = Authenticate(loginCredentials, deviceId, client, receiveKey, sendKey);

        var creds = new LoginCredentials
        {
            Username = welcome.CanonicalUsername,
            Typ = welcome.ReusableAuthCredentialsType,
            AuthData = welcome.ReusableAuthCredentials
        };

        return new SpotifyTcpConnection(creds, client, receiveKey, sendKey);
    }

    private static void Send(TcpClient client, ReadOnlyMemory<byte> sendKey, int nonce, SpotifyPacketType packageType,
        Span<byte> packet)
    {
        var stream = client.GetStream();
        const int MacLength = 4;
        const int HeaderLength = 3;

        var shannon = new Shannon(sendKey.Span);
        Span<byte> encoded = stackalloc byte[HeaderLength + packet.Length + MacLength];
        encoded[0] = (byte)packageType;

        BinaryPrimitives.WriteUInt16BigEndian(encoded[1..], (ushort)packet.Length);


        packet.CopyTo(encoded[3..]);
        shannon.Nonce((uint)nonce);

        shannon.Encrypt(encoded[..(3 + packet.Length)]);

        Span<byte> mac = stackalloc byte[MacLength];
        shannon.Finish(mac);

        mac.CopyTo(encoded[(3 + packet.Length)..]);
        stream.Write(encoded);
    }

    private static SpotifyPacketType Receive(TcpClient tcpClient, ReadOnlyMemory<byte> receiveKey, int nonce,
        out ReadOnlySpan<byte> output)
    {
        var stream = tcpClient.GetStream();

        var key = new Shannon(receiveKey.Span);
        Span<byte> header = new byte[3];
        stream.ReadExactly(header);
        key.Nonce((uint)nonce);
        key.Decrypt(header);

        var payloadLength = (short)((header[1] << 8) | (header[2] & 0xFF));
        Span<byte> x = new byte[payloadLength];
        stream.ReadExactly(x);
        key.Decrypt(x);

        Span<byte> mac = stackalloc byte[4];
        stream.ReadExactly(mac);

        Span<byte> expectedMac = stackalloc byte[4];
        key.Finish(expectedMac);

        if (!mac.SequenceEqual(expectedMac))
        {
            throw new InvalidConstraintException();
            //  throw new Exception("MAC mismatch");
        }

        output = x;
        return (SpotifyPacketType)header[0];
    }

    private static APWelcome Authenticate(LoginCredentials credentials,
        string deivceId,
        TcpClient client,
        ReadOnlyMemory<byte> receiveKey,
        ReadOnlyMemory<byte> sendKey)
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
            LoginCredentials = credentials,
            SystemInfo = new SystemInfo
            {
                CpuFamily = cpu,
                Os = os,
                SystemInformationString = "librespot 0.5.0-dev a211ff9",
                DeviceId = deivceId
            },
            VersionString = "librespot 0.5.0-dev a211ff9",
        }.ToByteArray();

        Send(client, sendKey, 0, SpotifyPacketType.Login, packet);
        var packageType = Receive(client, receiveKey, 0, out var payload);

        if (packageType is SpotifyPacketType.AuthFailure)
        {
            var fail = APLoginFailed.Parser.ParseFrom(payload);
            throw new SpotifyException(SpotifyFailureReason.AuthFailure, fail.ErrorCode.ToString());
        }

        if (packageType is not SpotifyPacketType.APWelcome)
        {
            throw new SpotifyException(SpotifyFailureReason.AuthFailure,
                "Unexpected packet type. Expected APWelcome but got: " + packageType);
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
        var n = new BigInteger(SpotifyConstants.SERVER_KEY, true, true)
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
            throw new SpotifyException(SpotifyFailureReason.AuthFailure,
                "Failed to verify server signature. WARNING: Potential man in the middle attack!");
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
                    Version = SpotifyConstants.SPOTIFY_DESKTOP_VERSION
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
        _client?.Dispose();
    }
}