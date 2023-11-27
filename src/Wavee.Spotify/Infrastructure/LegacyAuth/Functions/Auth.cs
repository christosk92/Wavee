using System.Buffers.Binary;
using System.Data;
using System.Net.Sockets;
using Eum.Spotify;
using Google.Protobuf;
using Wavee.Spotify.Domain.Exceptions;

namespace Wavee.Spotify.Infrastructure.LegacyAuth.Functions;

/// <summary>
/// Provides methods for authenticating with Spotify.
/// </summary>
internal static class Auth
{
    public static APWelcome Authenticate(NetworkStream stream,
        SpotifyEncryptionKeys keys,
        LoginCredentials credentials,
        string deviceId)
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

        Send(stream, SpotifyPacketType.Login, packet, keys.SendKey.Span, 0);
        var response = Receive(stream, keys.ReceiveKey.Span, 0);

        switch (response.Type)
        {
            case SpotifyPacketType.APWelcome:
                return APWelcome.Parser.ParseFrom(response.Payload);
            case SpotifyPacketType.AuthFailure:
                throw new SpotifyLegacyAuthenticationException(APLoginFailed.Parser.ParseFrom(response.Payload));
        }

        throw new NotSupportedException("Unknown packet type");
    }


    internal static void Send(NetworkStream stream,
        SpotifyPacketType packageType,
        ReadOnlySpan<byte> packagePayload,
        ReadOnlySpan<byte> sendKey,
        int sequence)
    {
        const int MacLength = 4;
        const int HeaderLength = 3;

        var shannon = new Shannon(sendKey);
        Span<byte> encoded = stackalloc byte[HeaderLength + packagePayload.Length + MacLength];
        encoded[0] = (byte)packageType;

        BinaryPrimitives.WriteUInt16BigEndian(encoded[1..], (ushort)packagePayload.Length);


        packagePayload.CopyTo(encoded[3..]);
        shannon.Nonce((uint)sequence);

        shannon.Encrypt(encoded[..(3 + packagePayload.Length)]);

        Span<byte> mac = stackalloc byte[MacLength];
        shannon.Finish(mac);

        mac.CopyTo(encoded[(3 + packagePayload.Length)..]);
        stream.Write(encoded);
    }

    /// <summary>
    /// Does a blocking read on the stream and returns a <see cref="SpotifyUnencryptedPackage"/> if successful.
    /// </summary>
    /// <param name="stream">
    /// The stream to read from.
    /// </param>
    /// <param name="receiveKey">
    ///  A <see cref="ReadOnlySpan{T}"/> containing the receive key, used for shannon decryption.
    /// </param>
    /// <param name="sequence">
    ///  The current sequence number. Used as a nonce for shannon decryption.
    /// </param>
    /// <returns>
    ///  The received <see cref="SpotifyUnencryptedPackage"/>.
    /// </returns>
    /// <exception cref="InvalidSignatureResult">
    /// The mac of the received package did not match the expected mac.
    /// </exception>
    internal static SpotifyUnencryptedPackage Receive(NetworkStream stream, ReadOnlySpan<byte> receiveKey, int sequence)
    {
        var key = new Shannon(receiveKey);
        Span<byte> header = new byte[3];
        stream.ReadExactly(header);
        key.Nonce((uint)sequence);
        key.Decrypt(header);

        var payloadLength = (short)((header[1] << 8) | (header[2] & 0xFF));
        Span<byte> payload = new byte[payloadLength];
        stream.ReadExactly(payload);
        key.Decrypt(payload);

        Span<byte> mac = stackalloc byte[4];
        stream.ReadExactly(mac);

        Span<byte> expectedMac = stackalloc byte[4];
        key.Finish(expectedMac);

        if (!mac.SequenceEqual(expectedMac))
        {
            throw new InvalidConstraintException();
            //  throw new Exception("MAC mismatch");
        }

        return new SpotifyUnencryptedPackage
        {
            Type = (SpotifyPacketType)header[0],
            Payload = payload
        };
    }
}