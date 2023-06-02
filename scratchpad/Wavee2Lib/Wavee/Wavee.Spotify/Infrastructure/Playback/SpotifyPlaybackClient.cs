using System.Reactive.Linq;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Player;
using Wavee.Spotify.Infrastructure.Mercury;
using Wavee.Spotify.Infrastructure.Playback.Contracts;
using Wavee.Spotify.Infrastructure.Remote.Contracts;

namespace Wavee.Spotify.Infrastructure.Playback;

internal sealed class SpotifyPlaybackClient : ISpotifyPlaybackClient, IDisposable
{
    private Func<ISpotifyMercuryClient> _mercuryFactory;
    private Func<SpotifyLocalPlaybackState, Task> _remoteUpdates;
    private readonly SpotifyPlaybackConfig _config;
    private readonly IDisposable _stateUpdatesSubscription;
    private readonly SpotifyRemoteConfig _remoteConfig;

    public SpotifyPlaybackClient(Func<ISpotifyMercuryClient> mercuryFactory,
        Func<SpotifyLocalPlaybackState, Task> remoteUpdates,
        SpotifyPlaybackConfig config,
        string deviceId,
        SpotifyRemoteConfig remoteConfig)
    {
        _mercuryFactory = mercuryFactory;
        _remoteUpdates = remoteUpdates;
        _config = config;
        _remoteConfig = remoteConfig;
        _stateUpdatesSubscription = WaveePlayer.Instance.StateUpdates
            .SelectMany(async x =>
            {
                if (x.TrackId.IsNone || x.TrackId.ValueUnsafe().Service is not ServiceType.Spotify)
                {
                    //immediatly create new empty state, and set no playback
                    await _remoteUpdates(SpotifyLocalPlaybackState.Empty(_remoteConfig, deviceId));
                    return default(Unit);
                }

                return default(Unit);
            }).Subscribe();
    }

    public Task<Unit> Play(string contextUri, Option<int> indexInContext, CancellationToken ct = default)
    {
        return Task.FromResult(result: default(Unit));
    }

    public Task OnPlaybackEvent(RemoteSpotifyPlaybackEvent ev)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
#pragma warning disable CS8625
        _mercuryFactory = null;
        _remoteUpdates = null;
#pragma warning restore CS8625
    }
}