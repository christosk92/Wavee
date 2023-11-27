using Eum.Spotify;
using Mediator;
using Wavee.Spotify.Application.Common.Queries;

namespace Wavee.Spotify.Infrastructure.LegacyAuth.Functions;

internal static class
    SpotifyLegacyAuth
{
    public static APWelcome Create(
        string host,
        ushort port,
        LoginCredentials credentials,
        string deviceId)
    {
        using var tcp = TcpIO.Connect(host, port);
        var stream = tcp.GetStream();
        var keys = Handshake.PerformHandshake(stream);
        var welcomeMessage = Auth.Authenticate(stream, keys, credentials, deviceId);

        return welcomeMessage;
    }
}