using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Eum.Spotify;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Wavee.Interfaces;

namespace Wavee.Services.Session;

internal sealed class Handshake
{
    internal static (byte[] ReceiveKey, byte[] SendKey, Stream stream) Do(ITcpClient tcpClient,
        ILogger<ITcpClient> logger)
    {
        logger.LogInformation("Starting handshake");
        Span<byte> private_key_bytes = stackalloc byte[95 * 8];
        RandomNumberGenerator.Fill(private_key_bytes);

        var privateKey = new BigInteger(private_key_bytes, true, true);
        var publicKey = BigInteger.ModPow(
            DH_GENERATOR,
            privateKey,
            DH_PRIME);

        
        var stream = tcpClient.GetStream();
        stream.ReadTimeout = (int)TimeSpan.FromSeconds(10).TotalMilliseconds;
        Span<byte> nonce = stackalloc byte[0x10];
        RandomNumberGenerator.Fill(nonce);

        var clientHello = NewClientHello(publicKey, nonce);
        logger.LogTrace("Sending client hello, {ClientHello}", ToHexString(clientHello));

        //Reduce number of copy operations by writing the header and payload in one go
        Span<byte> clientHelloSend = stackalloc byte[2 + sizeof(uint) + clientHello.Length];
        clientHelloSend[0] = 0;
        clientHelloSend[1] = 4;

        BinaryPrimitives.WriteUInt32BigEndian(clientHelloSend[2..], (uint)(2 + sizeof(uint) + clientHello.Length));
        clientHello.CopyTo(clientHelloSend.Slice(2 + sizeof(uint), clientHello.Length));

        stream.Write(clientHelloSend);

        // Read the response
        logger.LogTrace("Reading APResponse header");
        Span<byte> header = stackalloc byte[4];
        stream.ReadExactly(header);
        var size = BinaryPrimitives.ReadUInt32BigEndian(header);
        //Log: Read APResponse header, size: {size}
        logger.LogTrace("Reading APResponse body, size: {Size}", size - 4);
        Span<byte> receiveApMessage = stackalloc byte[(int)size - 4];
        stream.ReadExactly(receiveApMessage);

        logger.LogTrace("Received APResponse, {ApResponse}", ToHexString(receiveApMessage));

        var apResponse = APResponseMessage.Parser.ParseFrom(receiveApMessage);
        // Prevent man-in-the-middle attacks: check server signature
        var n = new BigInteger(SERVER_KEY, true, true)
            .ToByteArray(true, true);
        var e = new BigInteger(65537).ToByteArray(true, true);

        using var rsa = new RSACryptoServiceProvider();
        var rsaKeyInfo = new RSAParameters
        {
            Modulus = n,
            Exponent = e
        };
        rsa.ImportParameters(rsaKeyInfo);

        logger.LogTrace("Verifying server signature");
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
            throw new CryptographicException("Server signature is invalid");
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

        logger.LogTrace("Sending client response.");
        stream.Write(buffer);

        stream.ReadTimeout = Timeout.Infinite;

        // Log: Handshake complete
        logger.LogInformation("Handshake complete");
        return (x.ReceiveKey, x.SendKey, stream);
    }

    private static string ToHexString(ReadOnlySpan<byte> clientHello)
    {
        var sb = new StringBuilder();
        foreach (var b in clientHello)
        {
            sb.Append(b.ToString("X2"));
        }

        return sb.ToString();
    }

    private static (byte[] ReceiveKey, byte[] SendKey) compute_keys(
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
        var sendKey = data.Slice(0x14, 0x20).ToArray();
        var recvKey = data.Slice(0x34, 0x20).ToArray();


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
                    Version = SPOTIFY_DESKTOP_VERSION
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

    private const ulong SPOTIFY_DESKTOP_VERSION = 117300517;

    private static byte[] SERVER_KEY =
    {
        0xac, 0xe0, 0x46, 0x0b, 0xff, 0xc2, 0x30, 0xaf, 0xf4, 0x6b, 0xfe, 0xc3, 0xbf, 0xbf, 0x86, 0x3d,
        0xa1, 0x91, 0xc6, 0xcc, 0x33, 0x6c, 0x93, 0xa1, 0x4f, 0xb3, 0xb0, 0x16, 0x12, 0xac, 0xac, 0x6a,
        0xf1, 0x80, 0xe7, 0xf6, 0x14, 0xd9, 0x42, 0x9d, 0xbe, 0x2e, 0x34, 0x66, 0x43, 0xe3, 0x62, 0xd2,
        0x32, 0x7a, 0x1a, 0x0d, 0x92, 0x3b, 0xae, 0xdd, 0x14, 0x02, 0xb1, 0x81, 0x55, 0x05, 0x61, 0x04,
        0xd5, 0x2c, 0x96, 0xa4, 0x4c, 0x1e, 0xcc, 0x02, 0x4a, 0xd4, 0xb2, 0x0c, 0x00, 0x1f, 0x17, 0xed,
        0xc2, 0x2f, 0xc4, 0x35, 0x21, 0xc8, 0xf0, 0xcb, 0xae, 0xd2, 0xad, 0xd7, 0x2b, 0x0f, 0x9d, 0xb3,
        0xc5, 0x32, 0x1a, 0x2a, 0xfe, 0x59, 0xf3, 0x5a, 0x0d, 0xac, 0x68, 0xf1, 0xfa, 0x62, 0x1e, 0xfb,
        0x2c, 0x8d, 0x0c, 0xb7, 0x39, 0x2d, 0x92, 0x47, 0xe3, 0xd7, 0x35, 0x1a, 0x6d, 0xbd, 0x24, 0xc2,
        0xae, 0x25, 0x5b, 0x88, 0xff, 0xab, 0x73, 0x29, 0x8a, 0x0b, 0xcc, 0xcd, 0x0c, 0x58, 0x67, 0x31,
        0x89, 0xe8, 0xbd, 0x34, 0x80, 0x78, 0x4a, 0x5f, 0xc9, 0x6b, 0x89, 0x9d, 0x95, 0x6b, 0xfc, 0x86,
        0xd7, 0x4f, 0x33, 0xa6, 0x78, 0x17, 0x96, 0xc9, 0xc3, 0x2d, 0x0d, 0x32, 0xa5, 0xab, 0xcd, 0x05,
        0x27, 0xe2, 0xf7, 0x10, 0xa3, 0x96, 0x13, 0xc4, 0x2f, 0x99, 0xc0, 0x27, 0xbf, 0xed, 0x04, 0x9c,
        0x3c, 0x27, 0x58, 0x04, 0xb6, 0xb2, 0x19, 0xf9, 0xc1, 0x2f, 0x02, 0xe9, 0x48, 0x63, 0xec, 0xa1,
        0xb6, 0x42, 0xa0, 0x9d, 0x48, 0x25, 0xf8, 0xb3, 0x9d, 0xd0, 0xe8, 0x6a, 0xf9, 0x48, 0x4d, 0xa1,
        0xc2, 0xba, 0x86, 0x30, 0x42, 0xea, 0x9d, 0xb3, 0x08, 0x6c, 0x19, 0x0e, 0x48, 0xb3, 0x9d, 0x66,
        0xeb, 0x00, 0x06, 0xa2, 0x5a, 0xee, 0xa1, 0x1b, 0x13, 0x87, 0x3c, 0xd7, 0x19, 0xe6, 0x55, 0xbd
    };
}