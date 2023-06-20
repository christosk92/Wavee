using Eum.Spotify;
using Wavee.Infrastructure;
using Wavee.Spotify.Infrastructure.Authentication;

namespace Wavee.Spotify.Infrastructure.Connection;

internal sealed class SpotifyConnectionAccessor
{
    private object _lock = new();
    private SpotifyConnection? _connection;
    private readonly LoginCredentials _credentials;

    public SpotifyConnectionAccessor(LoginCredentials credentials)
    {
        _credentials = credentials;
    }

    public SpotifyConnection Access()
    {
        lock (_lock)
        {
            if (_connection is not null)
            {
                return _connection;
            }

            _connection = New(_credentials);
            return _connection;
        }
    }

    private static SpotifyConnection New(LoginCredentials credentials)
    {
        var tcpClient = TcpIO.Connect("ap-gae2.spotify.com", 4070);
        var stream = tcpClient.GetStream();
        var keys = Handshake.Handshake.PerformHandshake(stream);
        var deviceId = Guid.NewGuid().ToString();
        var conn = Auth.Authenticate(stream, keys, credentials, deviceId);
        return conn;
    }
}