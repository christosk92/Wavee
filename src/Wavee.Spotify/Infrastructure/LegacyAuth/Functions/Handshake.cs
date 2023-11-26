using System.Buffers.Binary;
using System.Data;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;
using Eum.Spotify;
using Google.Protobuf;
using Wavee.Spotify.Application.LegacyAuth.CommandHandlers;

namespace Wavee.Spotify.Infrastructure.LegacyAuth.Functions;

/// <summary>
/// Static class containing methods for performing the Spotify handshake.
/// </summary>
internal static class Handshake
{
    /// <summary>
    /// Performs the Spotify handshake by sending a client hello and receiving an AP response.
    /// The AP response is then verified and the encryption keys are computed.
    /// </summary>
    /// <param name="stream">
    ///  The <see cref="NetworkStream"/> to use for the handshake.
    /// </param>
    /// <returns>
    /// The encryption keys computed from the handshake.
    /// </returns>
    /// <exception cref="InvalidSignatureResult">
    ///  Thrown when the AP response signature is invalid. This usually means that the connection is being intercepted.
    /// </exception>
    public static SpotifyEncryptionKeys PerformHandshake(NetworkStream stream)
    {
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

        return new(computedKeys.SendKey, computedKeys.ReceiveKey);
    }

    private static SpotifyAuthenticationKeys compute_keys(ReadOnlyMemory<byte> sharedKey,
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


        return new SpotifyAuthenticationKeys(
            sendKey,
            recvKey,
            challenge);
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


    public readonly struct SpotifyAuthenticationKeys
    {
        public readonly ReadOnlyMemory<byte> SendKey;
        public readonly ReadOnlyMemory<byte> ReceiveKey;
        public readonly ReadOnlyMemory<byte> Challenge;

        public SpotifyAuthenticationKeys(ReadOnlyMemory<byte> sendKey, ReadOnlyMemory<byte> receiveKey,
            ReadOnlyMemory<byte> challenge)
        {
            SendKey = sendKey;
            ReceiveKey = receiveKey;
            Challenge = challenge;
        }
    }
}