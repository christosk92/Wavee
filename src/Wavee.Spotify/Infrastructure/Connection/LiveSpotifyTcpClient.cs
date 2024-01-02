using System.Buffers.Binary;
using System.Data;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;
using Eum.Spotify;
using Google.Protobuf;
using Wavee.Spotify.Core.Cryptography;
using Wavee.Spotify.Core.Models.Connection;
using Wavee.Spotify.Interfaces.Connection;

namespace Wavee.Spotify.Infrastructure.Connection;

internal sealed class LiveSpotifyTcpClient : ISpotifyTcpClient
{
    private readonly TcpClient _client;

    private SpotifyEncryptionKeys? _keys;
    private NetworkStream? _stream;
    private APWelcome? _welcomeMessage;

    public LiveSpotifyTcpClient()
    {
        _client = new TcpClient();
    }

    public async Task<APWelcome> ConnectAsync(string host,
        int port,
        LoginCredentials credentiasl, string deviceId)
    {
        await _client.ConnectAsync(host, port);
        var stream = _client.GetStream();
        _stream = stream;
        var keys = PerformHandshake(stream);
        _keys = keys;
        _welcomeMessage = PerformAuthentication(credentiasl, deviceId);

        return _welcomeMessage;
    }

    public bool Connected => _client.Connected 
                             && _stream is not null 
                             && _keys is not null 
                             && _welcomeMessage is not null;

    public SpotifyRefPackage Receive(int seq)
    {
        var receiveKey = _keys!.Value.ReceiveKey.Span;
        
        var key = new Shannon(receiveKey);
        Span<byte> header = new byte[3];
        _stream!.ReadExactly(header);
        key.Nonce((uint)seq);
        key.Decrypt(header);

        var payloadLength = (short)((header[1] << 8) | (header[2] & 0xFF));
        Span<byte> payload = new byte[payloadLength];
        _stream.ReadExactly(payload);
        key.Decrypt(payload);

        Span<byte> mac = stackalloc byte[4];
        _stream.ReadExactly(mac);

        Span<byte> expectedMac = stackalloc byte[4];
        key.Finish(expectedMac);

        if (!mac.SequenceEqual(expectedMac))
        {
            throw new InvalidConstraintException();
            //  throw new Exception("MAC mismatch");
        }

        return new SpotifyRefPackage
        {
            Type = (SpotifyPacketType)header[0],
            Data = payload
        };
    }

    public void Send(SpotifyRefPackage package, int seq)
    {
        const int MacLength = 4;
        const int HeaderLength = 3;

        var sendKey = _keys!.Value.SendKey.Span;
        var shannon = new Shannon(sendKey);
        Span<byte> encoded = stackalloc byte[HeaderLength + package.Data.Length + MacLength];
        encoded[0] = (byte)package.Type;

        BinaryPrimitives.WriteUInt16BigEndian(encoded[1..], (ushort)package.Data.Length);


        package.Data.CopyTo(encoded[3..]);
        shannon.Nonce((uint)seq);

        shannon.Encrypt(encoded[..(3 + package.Data.Length)]);

        Span<byte> mac = stackalloc byte[MacLength];
        shannon.Finish(mac);

        mac.CopyTo(encoded[(3 + package.Data.Length)..]);
        _stream!.Write(encoded);
    }

    private APWelcome PerformAuthentication(
        LoginCredentials ccr,
        string ddv)
    {
        // Implement authentication logic
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
            LoginCredentials = ccr,
            SystemInfo = new SystemInfo
            {
                CpuFamily = cpu,
                Os = os,
                SystemInformationString = "librespot 0.5.0-dev a211ff9",
                DeviceId = ddv
            },
            VersionString = "librespot 0.5.0-dev a211ff9",
        }.ToByteArray();

        Send(new SpotifyRefPackage
        {
            Type = SpotifyPacketType.Login,
            Data = packet
        }, 0);
        var response = Receive(0);

        switch (response.Type)
        {
            case SpotifyPacketType.APWelcome:
                return APWelcome.Parser.ParseFrom(response.Data);
            case SpotifyPacketType.AuthFailure:
            {
                var authFailure = APLoginFailed.Parser.ParseFrom(response.Data);
                break;
            }
        }

        throw new NotSupportedException("Unknown packet type");
    }

    private static SpotifyEncryptionKeys PerformHandshake(NetworkStream stream)
    {
        // Implement handshake logic
        var keys = DhLocalKeys.Random();
        var clientHello = BuildClientHello(keys);
        stream.Write(clientHello);

        Span<byte> header = stackalloc byte[4];
        stream.ReadExactly(header);
        var size = BinaryPrimitives.ReadUInt32BigEndian(header);
        Span<byte> payload = stackalloc byte[(int)size - 4];
        stream.ReadExactly(payload);
        var apResponse = APResponseMessage.Parser.ParseFrom(payload);

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

        var shared_secret = keys
            .SharedSecret(apResponse
                .Challenge
                .LoginCryptoChallenge
                .DiffieHellman
                .Gs.Span);
        //the accumulator is a collection of:
        //1) full client hello packet
        //2) apresponse header
        //3) apresponse body
        var computedKeys = compute_keys(shared_secret,
            clientHello,
            header,
            payload);

        ReadOnlySpan<byte> packet = new ClientResponsePlaintext
        {
            LoginCryptoResponse = new LoginCryptoResponseUnion
            {
                DiffieHellman = new LoginCryptoDiffieHellmanResponse
                {
                    Hmac = ByteString.CopyFrom(computedKeys.Challenge.Span)
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

        return computedKeys;
    }

    private static SpotifyEncryptionKeys compute_keys(ReadOnlyMemory<byte> sharedKey,
        ReadOnlySpan<byte> clientHello,
        Span<byte> apResponseHeader,
        Span<byte> apResponseBody)
    {
        // Solve challenge
        var sharedKeyBytes = sharedKey.ToArray();

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
        ReadOnlyMemory<byte> challenge = hmacFinal.ComputeHash(packets.ToArray());
        ReadOnlyMemory<byte> sendKey = data.Slice(0x14, 0x20).ToArray();
        ReadOnlyMemory<byte> recvKey = data.Slice(0x34, 0x20).ToArray();


        return new SpotifyEncryptionKeys(
            sendKey,
            recvKey,
            challenge);
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

    private static ReadOnlySpan<byte> BuildClientHello(DhLocalKeys keys)
    {
        Span<byte> nonce = stackalloc byte[0x10];
        RandomNumberGenerator.Fill(nonce);

        var payload = NewClientHello(keys.PublicKey, nonce);

        //Reduce number of copy operations by writing the header and payload in one go
        Span<byte> totalPacket = new byte[2 + sizeof(uint) + payload.Length];
        totalPacket[0] = 0;
        totalPacket[1] = 4;

        BinaryPrimitives.WriteUInt32BigEndian(totalPacket.Slice(2), (uint)(2 + sizeof(uint) + payload.Length));
        payload.CopyTo(totalPacket.Slice(2 + sizeof(uint), payload.Length));

        return totalPacket;
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}