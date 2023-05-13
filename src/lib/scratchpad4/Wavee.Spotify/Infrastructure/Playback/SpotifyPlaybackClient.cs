using Wavee.Core.Id;
using Wavee.Core.Infrastructure.Traits;
using Wavee.Spotify.Remote.Infrastructure;

namespace Wavee.Spotify.Infrastructure.Playback;

internal class SpotifyPlaybackClient<R> : ISpotifyPlaybackClient where R : struct, HasAudioOutput<R>, HasWebsocket<R>, HasLog<R>, HasHttp<R>
{
    private readonly SpotifyConnection<R> _connection;
    private readonly SpotifyRemoteConnection<R> _remoteConnection;
    public SpotifyPlaybackClient(SpotifyConnection<R> connection,
        SpotifyRemoteConnection<R> remoteConnection)
    {
        _connection = connection;
        _remoteConnection = remoteConnection;
    }
    
    public Task PlayContext(
        string contextUri,
        int indexInContext,
        TimeSpan position,
        bool startPlaying,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task PlayTrack(
        AudioId id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}