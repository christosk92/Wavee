using System.Buffers.Binary;
using System.Data;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;
using Eum.Spotify;
using Google.Protobuf;
using Wavee.Spfy.Exceptions;
using Wavee.Spfy.Utils;

namespace Wavee.Spfy.DefaultServices;

internal sealed class DefaultTcpClient : ITcpClient
{
    private bool _disposed;
    private TcpClient _tcpClient;
    private readonly Guid _instanceId;

    public DefaultTcpClient(Guid instanceId)
    {
        _instanceId = instanceId;
        _tcpClient = new TcpClient();
    }

    public bool IsConnected => !_disposed && _tcpClient is
    {
        Connected: true, Client.Connected: true
    };

    public ValueTask Connect(string host, ushort port)
    {
        return new ValueTask(_tcpClient.ConnectAsync(host, port));
    }

    public void Handshake()
    {
        Span<byte> private_key_bytes = stackalloc byte[95 * 8];
        RandomNumberGenerator.Fill(private_key_bytes);

        var privateKey = new BigInteger(private_key_bytes, true, true);
        var publicKey = BigInteger.ModPow(
            DH_GENERATOR,
            privateKey,
            DH_PRIME);

        var stream = _tcpClient.GetStream();
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

        EntityManager.SaveConnection(_instanceId, this, x.SendKey, x.ReceiveKey);
    }

    public APWelcome Authenticate(LoginCredentials credentials, string deviceId)
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
                DeviceId = deviceId
            },
            VersionString = "librespot 0.5.0-dev a211ff9",
        }.ToByteArray();

        Send(SpotifyPacketType.Login, packet);
        var packageType = Receive(out var payload);

        switch (packageType)
        {
            case SpotifyPacketType.APWelcome:
            {
                var welcomeMessage = APWelcome.Parser.ParseFrom(payload);
                EntityManager.SaveWelcomeMessage(_instanceId, welcomeMessage);
                return welcomeMessage;
            }
            case SpotifyPacketType.AuthFailure:
                throw new SpotifyAuthenticationException(APLoginFailed.Parser.ParseFrom(payload));
        }

        throw new NotSupportedException("Unknown packet type");
    }

    public void Send(SpotifyPacketType packageType, ReadOnlySpan<byte> packagePayload)
    {
        var stream = _tcpClient.GetStream();
        const int MacLength = 4;
        const int HeaderLength = 3;

        if (!EntityManager.TryGetSendKey(_instanceId, out var sendKey, out var sendSeq))
        {
            throw new Exception("Connection is not initialized");
        }

        var shannon = new Shannon(sendKey.Span);
        Span<byte> encoded = stackalloc byte[HeaderLength + packagePayload.Length + MacLength];
        encoded[0] = (byte)packageType;

        BinaryPrimitives.WriteUInt16BigEndian(encoded[1..], (ushort)packagePayload.Length);


        packagePayload.CopyTo(encoded[3..]);
        shannon.Nonce((uint)sendSeq);

        shannon.Encrypt(encoded[..(3 + packagePayload.Length)]);

        Span<byte> mac = stackalloc byte[MacLength];
        shannon.Finish(mac);

        mac.CopyTo(encoded[(3 + packagePayload.Length)..]);
        stream.Write(encoded);
    }

    public SpotifyPacketType Receive(out ReadOnlySpan<byte> payload)
    {
        var stream = _tcpClient.GetStream();

        if (!EntityManager.TryGetReceiveKey(_instanceId, out var recvKey, out var recvSeq))
        {
            throw new Exception("Connection is not initialized");
        }


        var key = new Shannon(recvKey.Span);
        Span<byte> header = new byte[3];
        stream.ReadExactly(header);
        key.Nonce((uint)recvSeq);
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

        payload = x;
        return (SpotifyPacketType)header[0];
    }

    public void Dispose()
    {
        _tcpClient?.Dispose();
        _disposed = true;
        _tcpClient = null;
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
}

[Flags]
internal enum SpotifyPacketType : byte
{
    SecretBlock = 0x02,
    Ping = 0x04,
    StreamChunk = 0x08,
    StreamChunkRes = 0x09,
    ChannelError = 0x0a,
    ChannelAbort = 0x0b,
    RequestKey = 0x0c,
    AesKey = 0x0d,
    AesKeyError = 0x0e,
    Image = 0x19,
    CountryCode = 0x1b,
    Pong = 0x49,
    PongAck = 0x4a,
    Pause = 0x4b,
    ProductInfo = 0x50,
    LegacyWelcome = 0x69,
    LicenseVersion = 0x76,
    Login = 0xab,
    APWelcome = 0xac,
    AuthFailure = 0xad,
    MercuryReq = 0xb2,
    MercurySub = 0xb3,
    MercuryUnsub = 0xb4,
    MercuryEvent = 0xb5,
    TrackEndedTime = 0x82,
    UnknownDataAllZeros = 0x1f,
    PreferredLocale = 0x74,
    Unknown0x0f = 0x0f,
    Unknown0x10 = 0x10,
    Unknown0x4f = 0x4f,

    // TODO - occurs when subscribing with an empty URI. Maybe a MercuryError?
    // Payload: b"\0\x08\0\0\0\0\0\0\0\0\x01\0\x01\0\x03 \xb0\x06"
    Unknown0xb6 = 0xb6,
}