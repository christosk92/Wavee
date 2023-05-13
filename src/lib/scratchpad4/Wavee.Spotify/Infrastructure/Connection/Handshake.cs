using System.Buffers.Binary;
using System.Diagnostics.Contracts;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Eum.Spotify;
using Google.Protobuf;
using Wavee.Core.Infrastructure.Sys.IO;
using Wavee.Core.Infrastructure.Traits;
using Wavee.Spotify.Infrastructure.Crypto;

namespace Wavee.Spotify.Infrastructure.Connection;

internal static class Handshake<RT>
    where RT : struct, HasTCP<RT>
{
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static LanguageExt.Aff<RT, SpotifyEncryptionRecord> PerformClientHello(NetworkStream stream,
        CancellationToken ct)
    {
        var keys = DhLocalKeys.Random();
        var clientHello = BuildClientHello(keys);
     
        return
            from _ in Tcp<RT>.Write(stream, clientHello)
            from apResponse in ReadApResponse(stream)
            let receivedKeysAndChallenge = VerifyApResponse(apResponse, clientHello, keys)
            from __ in Tcp<RT>.Write(stream, BuildApResponsePacket(receivedKeysAndChallenge.Challenge))
            from ___ in Tcp<RT>.SetTimeout(stream, Timeout.Infinite)
            select new SpotifyEncryptionRecord(
                receivedKeysAndChallenge.SendKey,
                0,
                receivedKeysAndChallenge.ReceiveKey,
                0);
    }

    private static LanguageExt.Aff<RT, (ReadOnlyMemory<byte>, ReadOnlyMemory<byte>)> ReadApResponse(
            NetworkStream stream) =>
            from tcpIo in default(RT).TcpEff
            from header in Tcp<RT>.ReadExactly(stream, 4)
            let size = BinaryPrimitives.ReadUInt32BigEndian(header.Span)
            from data in Tcp<RT>.ReadExactly(stream, (int)size - 4)
            select ((ReadOnlyMemory<byte>)header, (ReadOnlyMemory<byte>)data);


        private static ReadOnlyMemory<byte> BuildApResponsePacket(ReadOnlyMemory<byte> challenge)
        {
            ReadOnlySpan<byte> packet = new ClientResponsePlaintext
            {
                LoginCryptoResponse = new LoginCryptoResponseUnion
                {
                    DiffieHellman = new LoginCryptoDiffieHellmanResponse
                    {
                        Hmac = ByteString.CopyFrom(challenge.Span)
                    }
                },
                PowResponse = new PoWResponseUnion(),
                CryptoResponse = new CryptoResponseUnion()
            }.ToByteArray();


            Memory<byte> buffer = new byte[4 + packet.Length];
            BinaryPrimitives.WriteUInt32BigEndian(buffer.Span, (uint)(4 + packet.Length));
            packet.CopyTo(buffer.Span[4..]);
            return buffer;
        }

        private static ReadOnlyMemory<byte> BuildClientHello(DhLocalKeys keys)
        {
            static ReadOnlySpan<byte> NewClientHello(BigInteger publicKey,
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

            Span<byte> nonce = stackalloc byte[0x10];
            RandomNumberGenerator.Fill(nonce);

            var payload = NewClientHello(keys.PublicKey, nonce);

            //Reduce number of copy operations by writing the header and payload in one go
            Memory<byte> totalPacket = new byte[2 + sizeof(uint) + payload.Length];
            totalPacket.Span[0] = 0;
            totalPacket.Span[1] = 4;

            BinaryPrimitives.WriteUInt32BigEndian(totalPacket.Slice(2).Span, (uint)(2 + sizeof(uint) + payload.Length));
            payload.CopyTo(totalPacket.Slice(2 + sizeof(uint), payload.Length).Span);

            return totalPacket;
        }

        private static (ReadOnlyMemory<byte> SendKey, ReadOnlyMemory<byte> ReceiveKey, ReadOnlyMemory<byte> Challenge)
            VerifyApResponse((ReadOnlyMemory<byte> Header, ReadOnlyMemory<byte> Payload) ap,
                ReadOnlyMemory<byte> clientHello, DhLocalKeys keys)
        {
            var apResponse = APResponseMessage.Parser.ParseFrom(ap.Payload.Span);

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
                        .Gs.Span,
                    apResponse.Challenge
                        .LoginCryptoChallenge
                        .DiffieHellman
                        .GsSignature
                        .Span,
                    HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1))
            {
                throw new InvalidSignatureResult();
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
                clientHello.Span,
                ap.Header.Span,
                ap.Payload.Span);

            return computedKeys;
        }

        private static (ReadOnlyMemory<byte> SendKey, ReadOnlyMemory<byte> ReceiveKey, ReadOnlyMemory<byte> Challenge)
            compute_keys(
                ReadOnlyMemory<byte> sharedKey,
                ReadOnlySpan<byte> clientHello,
                ReadOnlySpan<byte> apResponseHeader,
                ReadOnlySpan<byte> apResponseBody)
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

            return (
                sendKey,
                recvKey,
                challenge);
        }
    }