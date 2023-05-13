using System.Reactive.Linq;
using Wavee.Core.Infrastructure.Traits;
using Wavee.Spotify.Remote.Infrastructure;
using Wavee.Spotify.Remote.Models;

namespace Wavee.Spotify.Infrastructure.Remote;

internal readonly struct SpotifyRemote<R> : ISpotifyRemoteClient
    where R : struct, HasLog<R>, HasWebsocket<R>, HasHttp<R>, HasAudioOutput<R>
{
    private readonly SpotifyRemoteConnection<R> _connection;

    public SpotifyRemote(SpotifyRemoteConnection<R> connection)
    {
        _connection = connection;
    }

    public IObservable<SpotifyRemoteState> StateChanged =>
        _connection
            .OnClusterChange()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .Select(SpotifyRemoteState.From);

}