using System.Net.Sockets;
using Eum.Spotify;

namespace Wavee.Spotify.Infrastructure.LegacyAuth.Functions;

internal static class
    SpotifyLegacyAuth
{
    public static (APWelcome WelcomeMessage, TcpClient TcpClient, SpotifyEncryptionKeys Keys) Create(
        string host,
        ushort port,
        LoginCredentials credentials,
        string deviceId)
    {
        var tcp = TcpIO.Connect(host, port);
        var stream = tcp.GetStream();
        var keys = Handshake.PerformHandshake(stream);
        var welcomeMessage = Auth.Authenticate(stream, keys, credentials, deviceId);

        return (welcomeMessage, tcp, keys);
    }
}