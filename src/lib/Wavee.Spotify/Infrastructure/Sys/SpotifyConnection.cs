using System.Buffers.Binary;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Eum.Spotify;
using Google.Protobuf;
using LanguageExt.Common;
using Wavee.Spotify.Constants;
using Wavee.Spotify.Crypto;
using Wavee.Spotify.Exceptions;
using Wavee.Spotify.Infrastructure.Common;
using Wavee.Spotify.Infrastructure.Traits;
using Wavee.Spotify.Models.Internal;

namespace Wavee.Spotify.Infrastructure.Sys;

internal static class SpotifyConnection<RT>
    where RT : struct, HasCancel<RT>, HasTCP<RT>
{
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Aff<RT, (ReadOnlyMemory<byte> SendKey, ReadOnlyMemory<byte> ReceiveKey)> ConnectButNoAuthenticate(
        string host, ushort port) =>
        from ct in cancelToken<RT>()
        from _ in default(RT).TcpEff.MapAsync(e => e.Connect(host, port, ct))
        let keys = DhLocalKeys.Random()
        from clientHello in default(RT).TcpEff.MapAsync(e => SendClientHello(e, keys, ct))
        from apResponse in ReadAPResponse(ct)
        let receivedKeysAndChallenge = VerifyApResponse(apResponse, clientHello, keys)
        from __ in SendClientResponse(receivedKeysAndChallenge.Challenge, ct)
        from ___ in default(RT).TcpEff.Map(e => e.SetTimeout(Timeout.Infinite))
        select (receivedKeysAndChallenge.SendKey, receivedKeysAndChallenge.ReceiveKey);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Aff<RT, (APWelcome Welcome, Option<uint> SendNonce, Option<uint> Receivenonce)> Authenticate(
        string deviceId,
        LoginCredentials credentials, ReadOnlyMemory<byte> SendKey, ReadOnlyMemory<byte> ReceiveKey) =>
        from ct in cancelToken<RT>()
        let sendNocne = Option<uint>.None
        let receiveNonce = Option<uint>.None
        from newSendNonce in SendEncryptedPackage(BuildAPLoginPacket(deviceId, credentials), SendKey, sendNocne)
        from newReceiveNonce in ReceiveDecryptedPackage(ReceiveKey, receiveNonce)
            .Map(c =>
            {
                switch (c.Packet.Command)
                {
                    case PacketType.APWelcome:
                        var welcome = APWelcome.Parser.ParseFrom(c.Packet.Payload.Span);
                        return (welcome, c.NextSequence);
                    case PacketType.AuthFailure:
                        var apLoginFailed = APLoginFailed.Parser.ParseFrom(c.Packet.Payload.Span);
                        throw new SpotifyAuthenticationException(apLoginFailed);
                    default:
                        throw new NotSupportedException("Unexpected packet type");
                }
            })
        //from _ in default(RT).TcpEff.MapAsync(e => e.Write(BuildAPLoginPacket(deviceId, credentials), ct))
        select (newReceiveNonce.welcome, newSendNonce, newReceiveNonce.NextSequence);

    private static SpotifyPacket BuildAPLoginPacket(
        string deviceId,
        LoginCredentials credentials)
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

        string osString = Environment.OSVersion.Platform.ToString();
        var os = osString switch
        {
            "Android" => Os.Android,
            "FreeBSD" or "NetBSD" or "OpenBSD" => Os.Freebsd,
            "iOS" => Os.Iphone,
            "Linux" => Os.Linux,
            "macOS" => Os.Osx,
            "Windows" => Os.Windows,
            _ => Os.Unknown
        };

        ReadOnlyMemory<byte> packet = new ClientResponseEncrypted
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

        return new SpotifyPacket(PacketType.Login, packet);
    }

    private static Aff<RT, Unit> SendClientResponse(ReadOnlyMemory<byte> challenge,
        CancellationToken ct) =>
        from _ in default(RT).TcpEff.MapAsync(e => e.Write(BuildApResponsePacket(challenge), ct))
        select unit;

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

    /// <summary>
    /// Encrypts and sends a packet to the server
    /// </summary>
    /// <param name="packet">The packet to send.</param>
    /// <param name="encryptionKey">The key used to encrypt data. Retrieved from authentication.</param>
    /// <param name="sequence">The current sequence number.</param>
    /// <returns>The new sequence (incrementing)</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Aff<RT, Option<uint>> SendEncryptedPackage(
        SpotifyPacket packet,
        ReadOnlyMemory<byte> encryptionKey,
        Option<uint> sequence) =>
        from ct in cancelToken<RT>()
        from connected in default(RT).TcpEff.Map(e => e.Connected)
        let nextSequence = sequence.Match(
            Some: s => s,
            None: () => 0u
        )
        from r in connected
            ? default(RT).TcpEff.MapAsync(e => e.Write(packet.Encrypt(nextSequence, encryptionKey.Span), ct))
            : FailAff<RT, Unit>(Error.New(ErrorCodes.NOT_CONNECTED, "Not connected"))
        select Some(nextSequence + 1);


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Aff<RT, (SpotifyPacket Packet, Option<uint> NextSequence)> ReceiveDecryptedPackage(
        ReadOnlyMemory<byte> decryptionKey,
        Option<uint> sequence) =>
        from ct in cancelToken<RT>()
        from connected in default(RT).TcpEff.Map(e => e.Connected)
        let nextSequence = sequence.Match(
            Some: s => s,
            None: () => 0u
        )
        from packet in connected
            ? ReadAndDecryptPacket(nextSequence, decryptionKey)
            : FailAff<RT, SpotifyPacket>(Error.New(ErrorCodes.NOT_CONNECTED, "Not connected"))
        select (packet, Some(nextSequence + 1));

    private static Aff<RT, SpotifyPacket> ReadAndDecryptPacket(ulong nextSequence, ReadOnlyMemory<byte> keySpan) =>
        from ct in cancelToken<RT>()
        let key = new Shannon(keySpan.Span)
        from header in default(RT).TcpEff.MapAsync(e => e.Read(3, ct)).Map(c =>
        {
            key.Nonce((uint)nextSequence);
            key.Decrypt(c.Span);
            return c;
        })
        let payloadLength = (short)((header.Span[1] << 8) | (header.Span[2] & 0xFF))
        from payload in default(RT).TcpEff.MapAsync(e => e.Read(payloadLength, ct)).Map(c =>
        {
            key.Decrypt(c.Span);
            return c;
        })
        from mac in default(RT).TcpEff.MapAsync(e => e.Read(SpotifyPacket.MAC_SIZE, ct))
        from expectedMac in Eff(() =>
        {
            Memory<byte> m = new byte[SpotifyPacket.MAC_SIZE];
            key.Finish(m.Span);
            return m;
        })
        from _ in mac.Span.SequenceEqual(expectedMac.Span)
            ? SuccessEff<RT, Unit>(unit)
            : FailAff<RT, Unit>(Error.New(ErrorCodes.INVALID_MAC, "Invalid MAC"))
        select new SpotifyPacket((PacketType)header.Span[0], payload);


    #region Connection

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


    private static Aff<RT, (ReadOnlyMemory<byte>, ReadOnlyMemory<byte>)> ReadAPResponse(
        CancellationToken ct) =>
        from tcpIo in default(RT).TcpEff
        from header in tcpIo.Read(4, ct).ToAff()
        let size = BinaryPrimitives.ReadUInt32BigEndian(header.Span)
        from data in tcpIo.Read((int)size - 4, ct).ToAff()
        select ((ReadOnlyMemory<byte>)header, (ReadOnlyMemory<byte>)data);

    private static ValueTask<ReadOnlyMemory<byte>> SendClientHello(TcpIO tcpIo, DhLocalKeys keys,
        CancellationToken ct)
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

        Span<byte> nonce = stackalloc byte[0x10];
        RandomNumberGenerator.Fill(nonce);

        var payload = NewClientHello(keys.PublicKey, nonce);

        //Reduce number of copy operations by writing the header and payload in one go
        Memory<byte> totalPacket = new byte[2 + sizeof(uint) + payload.Length];
        totalPacket.Span[0] = 0;
        totalPacket.Span[1] = 4;

        BinaryPrimitives.WriteUInt32BigEndian(totalPacket.Slice(2).Span, (uint)(2 + sizeof(uint) + payload.Length));
        payload.CopyTo(totalPacket.Slice(2 + sizeof(uint), payload.Length).Span);

        return new ValueTask<ReadOnlyMemory<byte>>(SendClientHelloInternal(tcpIo, totalPacket, ct));
    }

    private static async Task<ReadOnlyMemory<byte>> SendClientHelloInternal(
        TcpIO tcp,
        ReadOnlyMemory<byte> totalPacket, CancellationToken ct = default)
    {
        await tcp.Write(totalPacket, ct);
        return totalPacket;
    }

    #endregion
}

internal sealed class InvalidSignatureResult : Exception
{
}