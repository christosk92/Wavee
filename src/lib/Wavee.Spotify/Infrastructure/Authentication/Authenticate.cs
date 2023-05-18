using System.Net.Sockets;
using Eum.Spotify;
using Google.Protobuf;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Spotify.Infrastructure.Connection.Crypto;
using Wavee.Spotify.Infrastructure.Tcp;

namespace Wavee.Spotify.Infrastructure.Authentication;

internal static class Authenticate
{
    public static SpotifyAuthenticationResult PerformAuth(
        NetworkStream stream,
        LoginCredentials credentials,
        SpotifyConnectionRecord connection,
        string deviceId)
    {
        connection = SendAuthenticationRequest(stream, connection, credentials, deviceId);
        var response = SpotifyTcp.Receive(stream, ref connection);
        var packetType = (SpotifyPacketType)response.Header[0];
        return packetType switch
        {
            SpotifyPacketType.APWelcome => new SpotifyAuthenticationResult(
                WelcomeMessage: APWelcome.Parser.ParseFrom(response.Payload),
                connection),
            SpotifyPacketType.AuthFailure => throw new SpotifyAuthenticationException(
                APLoginFailed.Parser.ParseFrom(response.Payload)),
        };
    }

    private static SpotifyConnectionRecord SendAuthenticationRequest(
        NetworkStream stream,
        SpotifyConnectionRecord connection,
        LoginCredentials credentials, string deviceId)
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

        return SpotifyTcp.Send(
            stream,
            connection,
            SpotifyPacketType.Login,
            packet);
    }
}