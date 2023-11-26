using Eum.Spotify;

namespace Wavee.Spotify.Infrastructure.LegacyAuth.Functions;

internal static class
    SpotifyLegacyAuth
{
    public static APWelcome Create(
        LoginCredentials credentials,
        string deviceId)
    {
        const string host = "ap-gae2.spotify.com";
        const ushort port = 4070;

        using var tcp = TcpIO.Connect(host, port);
        var stream = tcp.GetStream();
        var keys = Handshake.PerformHandshake(stream);
        var welcomeMessage = Auth.Authenticate(stream, keys, credentials, deviceId);

        return welcomeMessage;
    }
}