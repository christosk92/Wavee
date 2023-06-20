using NeoSmart.AsyncLock;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Spotify.Mercury;

namespace Wavee.Spotify.Remote.Live;

internal sealed class SpotifyRemoteConnectionAccessor
{
    private AsyncLock _lock = new();
    private SpotifyRemoteConnection? _connection;
    private readonly Func<IMercuryClient> _mercuryFactory;
    private readonly SpotifyConnectionAccessor _connectionFactory;

    public SpotifyRemoteConnectionAccessor(Func<IMercuryClient> mercuryFactory,
        SpotifyConnectionAccessor connectionFactory)
    {
        _mercuryFactory = mercuryFactory;
        _connectionFactory = connectionFactory;
    }

    public async ValueTask<SpotifyRemoteConnection> Access()
    {
        using (await _lock.LockAsync())
        {
            if (_connection is { IsClosed: false })
            {
                return _connection;
            }

            _connection?.Dispose();
            _connection = await Create();
            return _connection;
        }
    }

    private async Task<SpotifyRemoteConnection> Create(CancellationToken ct = default)
    {
        var connection = _connectionFactory.Access();
        var dealer = SpotifyConnectionAccessor.Dealer;

        return await SpotifyRemoteConnection.ConnectAsync(dealer, _mercuryFactory, connection, ct);
    }
}