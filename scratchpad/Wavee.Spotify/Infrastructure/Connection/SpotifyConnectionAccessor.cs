using Eum.Spotify;
using Wavee.Infrastructure;
using Wavee.Spotify.Infrastructure.Authentication;

namespace Wavee.Spotify.Infrastructure.Connection;

internal sealed class SpotifyConnectionAccessor
{
    private object _lock = new();
    private SpotifyConnection? _connection;
    private readonly LoginCredentials _credentials;
    private readonly SpotifyConfig _config;

    public SpotifyConnectionAccessor(LoginCredentials credentials, SpotifyConfig config)
    {
        _credentials = credentials;
        _config = config;
    }

    public static string SpClient => "gae2-spclient.spotify.com:443";
    public static string Dealer => "gae2-dealer.spotify.com:443";

    public SpotifyConnection Access()
    {
        lock (_lock)
        {
            if (_connection is not null)
            {
                return _connection;
            }

            _connection = New(_credentials, _config);
            return _connection;
        }
    }

    private static SpotifyConnection New(LoginCredentials credentials, SpotifyConfig config)
    {
        var tcpClient = TcpIO.Connect("ap-gae2.spotify.com", 4070);
        var stream = tcpClient.GetStream();
        var keys = Handshake.Handshake.PerformHandshake(stream);
        var deviceId = Guid.NewGuid().ToString();
        var conn = Auth.Authenticate(stream, keys, credentials, deviceId, config);
        return conn;
    }
}