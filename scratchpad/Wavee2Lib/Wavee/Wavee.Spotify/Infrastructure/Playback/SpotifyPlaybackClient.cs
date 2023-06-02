using System.Reactive.Linq;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Ids;
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
    private readonly Ref<Option<string>> _countryCode;
    private readonly TaskCompletionSource<Unit> _ready;
    
    public SpotifyPlaybackClient(Func<ISpotifyMercuryClient> mercuryFactory,
        Func<SpotifyLocalPlaybackState, Task> remoteUpdates,
        SpotifyPlaybackConfig config,
        string deviceId,
        SpotifyRemoteConfig remoteConfig,
        Ref<Option<string>> countryCode, TaskCompletionSource<Unit> ready)
    {
        _mercuryFactory = mercuryFactory;
        _remoteUpdates = remoteUpdates;
        _config = config;
        _remoteConfig = remoteConfig;
        _countryCode = countryCode;
        _ready = ready;

        object _stateLock = new object();
        bool isActive = false;
        bool activeChanged = false;
        SpotifyLocalPlaybackState previousState = default;
        _stateUpdatesSubscription = WaveePlayer.Instance.StateUpdates
            .SelectMany(async x =>
            {
                if (x.TrackId.IsNone || x.TrackId.ValueUnsafe().Service is not ServiceType.Spotify)
                {
                    //immediatly create new empty state, and set no playback
                    isActive = false;
                    previousState = SpotifyLocalPlaybackState.Empty(_remoteConfig, deviceId);
                    return previousState;
                }
                
                //wait for a connectionid
                await _ready.Task;
                
                lock (_stateLock)
                {
                    if (!isActive)
                    {
                        isActive = true;
                        activeChanged = true;
                    }

                    var ns = previousState.FromPlayer(x, isActive, activeChanged);
                    previousState = ns;
                    return ns;
                }
            })
            .SelectMany(async x =>
            {
                await _remoteUpdates(x);
                return default(Unit);
            })
            .Subscribe();
    }

    public async Task<Unit> Play(string contextUri, Option<int> indexInContext, CancellationToken ct = default)
    {
        var ctx = await BuildContext(contextUri, _mercuryFactory);
        await WaveePlayer.Instance.Play(ctx, indexInContext);
        return default;
    }

    public Task OnPlaybackEvent(RemoteSpotifyPlaybackEvent ev)
    {
        throw new NotImplementedException();
    }

    private async Task<WaveeContext> BuildContext(string contextUri, Func<ISpotifyMercuryClient> mercuryFactory)
    {
        var mercury = mercuryFactory();
        var context = await mercury.ContextResolve(contextUri);

        var futureTracks = BuildFutureTracks(context, _countryCode);

        return new WaveeContext(
            Id: contextUri,
            Name: context.Metadata.Find("context_description").IfNone(contextUri.ToString()),
            FutureTracks: futureTracks,
            ShuffleProvider: Option<IShuffleProvider>.None
        );
    }

    private IEnumerable<FutureWaveeTrack> BuildFutureTracks(SpotifyContext spotifyContext, Ref<Option<string>> country)
    {
        foreach (var page in spotifyContext.Pages)
        {
            //check if the page has tracks
            //if it does, yield return each track
            //if it doesn't, fetch the next page (if next page is set). if not go to the next page
            if (page.Tracks.Count > 0)
            {
                foreach (var track in page.Tracks)
                {
                    var id = AudioId.FromUri(track.Uri);
                    var uid = track.HasUid ? track.Uid : Option<string>.None;
                    var trackMetadata = track.Metadata.ToHashMap();

                    yield return new FutureWaveeTrack(id,
                        TrackUid: uid.IfNone(id.ToBase16()),
                        () => StreamTrack(id, trackMetadata, country.Value.IfNone("US")));
                }
            }
            else
            {
                //fetch the page if page url is set
                //if not, go to the next page
                if (page.HasPageUrl)
                {
                    var pageUrl = page.PageUrl;
                    var mercury = _mercuryFactory();
                    var pageResolve = mercury.ContextResolveRaw(pageUrl).ConfigureAwait(false)
                        .GetAwaiter().GetResult();
                    foreach (var track in BuildFutureTracks(pageResolve, country))
                    {
                        yield return track;
                    }
                }
                else if (page.HasNextPageUrl)
                {
                    var pageUrl = page.NextPageUrl;
                    var mercury = _mercuryFactory();

                    var pageResolve = mercury.ContextResolveRaw(pageUrl).ConfigureAwait(false)
                        .GetAwaiter().GetResult();
                    foreach (var track in BuildFutureTracks(pageResolve, country))
                    {
                        yield return track;
                    }
                }
            }
        }
    }

    private Task<WaveeTrack> StreamTrack(AudioId id,
        HashMap<string, string> trackMetadata,
        string country)
    {
        //dummy vmp3
        //"C:\Users\chris-pc\Music\Busker Busker - 처음엔 사랑이란게 (Love At First).mp3"
        var path = @"C:\Users\chris-pc\Music\Busker Busker - 처음엔 사랑이란게 (Love At First).mp3";
        return Task.FromResult(new WaveeTrack(
            audioStream: File.OpenRead(path),
            id: id,
            metadata: trackMetadata,
            title: "Busker Busker - 처음엔 사랑이란게 (Love At First)",
            duration: TimeSpan.FromSeconds(200)
        ));
    }

    public void Dispose()
    {
#pragma warning disable CS8625
        _mercuryFactory = null;
        _remoteUpdates = null;
#pragma warning restore CS8625
    }
}