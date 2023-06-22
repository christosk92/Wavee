using System.Net.Sockets;
using Eum.Spotify;
using Google.Protobuf;
using Wavee.Infrastructure.Connection;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Spotify.Infrastructure.Handshake;

namespace Wavee.Infrastructure.Authentication;

/// <summary>
/// Provides methods for authenticating with Spotify.
/// </summary>
internal static class Auth
{
    public static APWelcome Authenticate(NetworkStream stream,
        SpotifyEncryptionKeys keys,
        LoginCredentials credentials,
        string deviceId, SpotifyConfig config)
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

        var package = new SpotifyUnencryptedPackage(SpotifyPacketType.Login, packet);

        SpotifyConnection.Send(stream, package, keys.SendKey.Span, 0);
        var response = SpotifyConnection.Receive(stream, keys.ReceiveKey.Span, 0);

        switch (response.Type)
        {
            case SpotifyPacketType.APWelcome:
                return APWelcome.Parser.ParseFrom(response.Payload);
            case SpotifyPacketType.AuthFailure:
                throw new SpotifyAuthenticationException(APLoginFailed.Parser.ParseFrom(response.Payload));
        }

        throw new NotSupportedException("Unknown packet type");
    }
}