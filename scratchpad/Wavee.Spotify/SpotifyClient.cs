using System.Net.Sockets;
using Eum.Spotify;
using Wavee.Infrastructure;
using Wavee.Spotify.Artist;
using Wavee.Spotify.Artist.Live;
using Wavee.Spotify.Infrastructure.Authentication;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Spotify.Infrastructure.Handshake;
using Wavee.Spotify.InternalApi;
using Wavee.Spotify.InternalApi.Live;
using Wavee.Spotify.Mercury;
using Wavee.Spotify.Mercury.Live;
using Wavee.Spotify.Remote;
using Wavee.Spotify.Remote.Live;

namespace Wavee.Spotify;

public class SpotifyClient
{
    private readonly SpotifyConnectionAccessor _conn;

    private SpotifyClient(SpotifyConnectionAccessor conn)
    {
        _conn = conn;
        Mercury = new LiveMercuryClient(_conn);
        Remote = new SpotifyRemoteClient(() => Mercury, _conn, _conn.Access().DeviceId, CreateTokenFactory());
    }

    // ReSharper disable once HeapView.BoxingAllocation
    public IArtistClient Artist => new LiveArtistClient(api: InternalApi);

    // ReSharper disable once HeapView.BoxingAllocation
    public IInternalApi InternalApi => new LiveInternalApi(tokenFactory: CreateTokenFactory());

    public IMercuryClient Mercury { get; }
    public ISpotifyRemoteClient Remote { get; }

    private Func<CancellationToken, ValueTask<string>> CreateTokenFactory()
    {
        return Mercury.GetToken;
    }

    public static SpotifyClient Create(LoginCredentials credentials, SpotifyConfig config)
    {
        var conn = new SpotifyConnectionAccessor(credentials, config);
        return new SpotifyClient(conn);
    }
}

internal class Locked<T>
{
    private T _value;
    private readonly object _lock = new();

    private Locked(T value)
    {
        _value = value;
    }

    public static Locked<T> Create(T value) => new Locked<T>(value);

    public void Update(Action<T> update)
    {
        lock (_lock)
        {
            update(_value);
        }
    }

    public TResult Query<TResult>(Func<T, TResult> query)
    {
        lock (_lock)
        {
            return query(_value);
        }
    }

    public void Set(T value)
    {
        lock (_lock)
        {
            _value = value;
        }
    }

    public T GetUnsafe()
    {
        lock (_lock)
        {
            return _value;
        }
    }

    public void Update(Func<T, T> update)
    {
        lock (_lock)
        {
            _value = update(_value);
        }
    }
}