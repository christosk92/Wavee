using System.Net.Sockets;
using Eum.Spotify;
using Google.Protobuf;
using LanguageExt.Common;
using Wavee.Core.Infrastructure.Sys.IO;
using Wavee.Core.Infrastructure.Traits;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Spotify.Infrastructure.Crypto;

namespace Wavee.Spotify.Infrastructure.Authentication;

internal static class Authentication<RT>
    where RT : struct, HasTCP<RT>
{
    internal static Aff<RT, (APWelcome ApWelcome, SpotifyEncryptionRecord EncryptionRecord)> Authenticate(
        NetworkStream stream, SpotifyEncryptionRecord authRecord, LoginCredentials credentials, string deviceId) =>
        from ct in cancelToken<RT>()
        let authPackage = BuildAPLoginPacket(deviceId, credentials)
        from newRecord in SendEncryptedMessage(stream, authPackage, authRecord)
        from welcome in ReadDecryptedMessage(stream, newRecord)
            .Map(c =>
            {
                //check if the packet is an APWelcome
                switch (c.Packet.Command)
                {
                    case SpotifyPacketType.APWelcome:
                        return (APWelcome.Parser.ParseFrom(c.Packet.Data.Span), c.NewEncryptionRecord);
                    case SpotifyPacketType.AuthFailure:
                        throw new SpotifyAuthenticationException(APLoginFailed.Parser.ParseFrom(c.Packet.Data.Span));
                }

                throw new NotSupportedException("Unknown packet type");
            })
        select welcome;

    internal static Aff<RT, (SpotifyPacket Packet, SpotifyEncryptionRecord NewEncryptionRecord)> ReadDecryptedMessage(
        NetworkStream stream, SpotifyEncryptionRecord record) =>
        from ct in cancelToken<RT>()
        let key = new Shannon(record.DecryptionKey.Span)
        from header in Tcp<RT>.ReadExactly(stream, 3).Map(c =>
        {
            key.Nonce((uint)record.DecryptionNonce);
            key.Decrypt(c.Span);
            return c;
        })
        let payloadLength = (short)((header.Span[1] << 8) | (header.Span[2] & 0xFF))
        from payload in Tcp<RT>.ReadExactly(stream, payloadLength).Map(c =>
        {
            key.Decrypt(c.Span);
            return c;
        })
        from mac in Tcp<RT>.ReadExactly(stream, SpotifyEncryptionRecord.MAC_SIZE)
        from expectedMac in Eff(() =>
        {
            Memory<byte> m = new byte[SpotifyEncryptionRecord.MAC_SIZE];
            key.Finish(m.Span);
            return m;
        })
        from _ in mac.Span.SequenceEqual(expectedMac.Span)
            ? SuccessEff<RT, Unit>(unit)
            : FailAff<RT, Unit>(Error.New("Invalid MAC"))
        select (new SpotifyPacket((SpotifyPacketType)header.Span[0], payload), record with
        {
            DecryptionNonce = SpotifyEncryptionRecord.IncrementNonce(record.DecryptionNonce)
        });

    internal static Aff<RT, SpotifyEncryptionRecord> SendEncryptedMessage(NetworkStream stream, SpotifyPacket packet,
        SpotifyEncryptionRecord record) =>
        from ct in cancelToken<RT>()
        let data = record.EncryptPacket(packet)
        from _ in Tcp<RT>.Write(stream, data.EnrcyptedMessage)
        select data.NewRecord;

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

        string osString = Environment.OSVersion.Platform.ToString().ToLower();
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

        return new SpotifyPacket(SpotifyPacketType.Login, packet);
    }
}