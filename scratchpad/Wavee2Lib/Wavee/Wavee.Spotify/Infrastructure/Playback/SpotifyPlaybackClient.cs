using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reactive.Linq;
using System.Text;
using Eum.Spotify.storage;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Spotify.Metadata;
using Wavee.Core.Ids;
using Wavee.Infrastructure.IO;
using Wavee.Player;
using Wavee.Spotify.Infrastructure.ApResolve;
using Wavee.Spotify.Infrastructure.AudioKey;
using Wavee.Spotify.Infrastructure.Cache;
using Wavee.Spotify.Infrastructure.Mercury;
using Wavee.Spotify.Infrastructure.Playback.Contracts;
using Wavee.Spotify.Infrastructure.Remote.Contracts;

namespace Wavee.Spotify.Infrastructure.Playback;

internal sealed class SpotifyPlaybackClient : ISpotifyPlaybackClient, IDisposable
{
    private Func<ISpotifyCache> _cacheFactory;
    private Func<IAudioKeyProvider> _audioKeyProviderFactory;
    private Func<ISpotifyMercuryClient> _mercuryFactory;
    private Func<SpotifyLocalPlaybackState, Task> _remoteUpdates;
    private readonly SpotifyPlaybackConfig _config;
    private readonly IDisposable _stateUpdatesSubscription;
    private readonly IDisposable _positionUpdates;
    private readonly SpotifyRemoteConfig _remoteConfig;
    private readonly Ref<Option<string>> _countryCode;
    private readonly TaskCompletionSource<Unit> _ready;
    private SpotifyLocalPlaybackState previousState;
    public SpotifyPlaybackClient(
        Func<ISpotifyMercuryClient> mercuryFactory,
        Func<IAudioKeyProvider> audioKeyProviderFactory,
        Func<ISpotifyCache> cacheFactory,
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
        _audioKeyProviderFactory = audioKeyProviderFactory;
        _cacheFactory = cacheFactory;

        object _stateLock = new object();
        bool isActive = false;
        bool activeChanged = false;

        _positionUpdates = WaveePlayer.Instance.PositionUpdates
            .SelectMany(async x =>
            {
                if (!previousState.IsActive || x.IsNone)
                    return Option<SpotifyLocalPlaybackState>.None;

                await _ready.Task;

                lock (_stateLock)
                {
                    if (!isActive)
                    {
                        isActive = true;
                        activeChanged = true;
                    }

                    var ns = previousState.SetPosition(x.ValueUnsafe());
                    previousState = ns;
                    return Option<SpotifyLocalPlaybackState>.Some(ns);
                }
            })
            .SelectMany(async (x) =>
            {
                if (x.IsSome)
                {
                    await _remoteUpdates(x.ValueUnsafe());
                }

                return default(Unit);
            }).Subscribe();
        _stateUpdatesSubscription = WaveePlayer.Instance.StateUpdates
            .SelectMany(async x =>
            {
                if (x.TrackId.IsNone || x.TrackId.ValueUnsafe().Service is not ServiceType.Spotify)
                {
                    //immediatly create new empty state, and set no playback
                    isActive = false;
                    previousState = SpotifyLocalPlaybackState.Empty(_remoteConfig, deviceId);
                    return Option<SpotifyLocalPlaybackState>.Some(previousState);
                }

                //wait for a connectionid
                await _ready.Task;

                if (x.PermanentEnd)
                {
                    //start autoplay query
                    Debug.WriteLine("Permanent end");
                    var autoplay = await mercuryFactory().Autoplay(x.Context.ValueUnsafe().Id);
                    var nextContext = await BuildContext(autoplay, _mercuryFactory);
                    await WaveePlayer.Instance.Play(nextContext, 0, Option<TimeSpan>.None, false);
                    return Option<SpotifyLocalPlaybackState>.None;
                }

                lock (_stateLock)
                {
                    if (!isActive)
                    {
                        isActive = true;
                        activeChanged = true;
                    }

                    var ns = previousState.FromPlayer(x, isActive, activeChanged);
                    previousState = ns;
                    return Option<SpotifyLocalPlaybackState>.Some(ns);
                }
            })
            .SelectMany(async x =>
            {
                if (x.IsSome)
                {
                    await _remoteUpdates(x.ValueUnsafe());
                }

                return default(Unit);
            })
            .Subscribe();
    }

    public async Task<Unit> Play(string contextUri, Option<int> indexInContext, Option<TimeSpan> startFrom,
        CancellationToken ct = default)
    {
        var ctx = await BuildContext(contextUri, _mercuryFactory);
        await WaveePlayer.Instance.Play(ctx, indexInContext, startFrom, false);
        return default;
    }

    private async Task<Unit> PlayInternal(string contextUri,
        Option<string> trackUid,
        Option<int> indexInContext,
        AudioId trackId, TimeSpan from,
        bool startPaused)

    {
        var ctx = await BuildContext(contextUri, _mercuryFactory);

        int findCorrectIndex()
        {
            if (indexInContext.IsSome)
            {
                return indexInContext.ValueUnsafe();
            }

            int index = 0;
            foreach (var itemIn in ctx.FutureTracks)
            {
                if (trackUid.IsSome)
                {
                    if (itemIn.TrackUid == trackUid.ValueUnsafe())
                    {
                        return index;
                    }
                }
                else if (itemIn.TrackId == trackId)
                {
                    return index;
                }

                index++;
            }

            return index;
        }

        var idx = findCorrectIndex();
        await WaveePlayer.Instance.Play(ctx, idx, from, startPaused);
        return default;
    }

    public async Task OnPlaybackEvent(RemoteSpotifyPlaybackEvent ev)
    {
        switch (ev.EventType)
        {
            case RemoteSpotifyPlaybackEventType.Play:
                await PlayInternal(ev.ContextUri.ValueUnsafe(),
                    ev.TrackUid,
                    ev.TrackIndex,
                    ev.TrackId,
                    ev.PlaybackPosition,
                    ev.IsPaused);
                break;

            case RemoteSpotifyPlaybackEventType.SeekTo:
                previousState = previousState with
                {
                    LastCommandId = ev.CommandId.ValueUnsafe(),
                    LastCommandSentBy = ev.SentBy.ValueUnsafe()
                };
                var to = ev.SeekTo.IfNone(TimeSpan.Zero);
                WaveePlayer.Instance.SeekTo(to);
                break;
            case RemoteSpotifyPlaybackEventType.SkipNext:
                previousState = previousState with
                {
                    LastCommandId = ev.CommandId.ValueUnsafe(),
                    LastCommandSentBy = ev.SentBy.ValueUnsafe()
                };
                await WaveePlayer.Instance.SkipNext(false);
                break;
            case RemoteSpotifyPlaybackEventType.Pause:
                previousState = previousState with
                {
                    LastCommandId = ev.CommandId.ValueUnsafe(),
                    LastCommandSentBy = ev.SentBy.ValueUnsafe()
                };
                WaveePlayer.Instance.Pause();
                break;
            case RemoteSpotifyPlaybackEventType.Resume:
                previousState = previousState with
                {
                    LastCommandId = ev.CommandId.ValueUnsafe(),
                    LastCommandSentBy = ev.SentBy.ValueUnsafe()
                };
                WaveePlayer.Instance.Resume();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
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
                        (ct) => StreamTrack(id, trackMetadata, country.Value.IfNone("US"), ct));
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
        string country,
        CancellationToken ct)
    {
        var mercury = _mercuryFactory();
        var audioKeyProvider = _audioKeyProviderFactory();
        var cache = _cacheFactory();
        switch (id.Type)
        {
            case AudioItemType.Track:
                return StreamTrackSpecifically(id, trackMetadata, country, mercury, audioKeyProvider, cache, ct);
            case AudioItemType.PodcastEpisode:
                return StreamPodcastEpisodeSpecifically(id, trackMetadata, country, mercury);
        }

        throw new NotSupportedException("Cannot stream this type of audio item");
    }

    private Task<WaveeTrack> StreamPodcastEpisodeSpecifically(AudioId id, HashMap<string, string> trackMetadata,
        string country, ISpotifyMercuryClient mercury)
    {
        throw new NotImplementedException();
    }

    internal async Task<WaveeTrack> StreamTrackSpecifically(AudioId id,
        HashMap<string, string> trackMetadata,
        string country,
        ISpotifyMercuryClient mercury,
        IAudioKeyProvider audioKeyProvider,
        ISpotifyCache cache,
        CancellationToken ct)
    {
        var track = await mercury.GetTrack(id, ct);
        var preferedQuality = _config.PreferedQuality;
        var canPlay = CanPlay(track, country);
        if (!canPlay)
        {
            throw new NotSupportedException("Cannot play this track");
        }

        var format = GetBestFormat(track, preferedQuality);
        if (format is null)
        {
            throw new NotSupportedException("Cannot play this track");
        }

        var audioKeyRes = await audioKeyProvider.GetAudioKey(id, format, ct);
        var audoioKey = audioKeyRes.Match(
            Left: err => throw new Exception($"Could not get audio key: {err}"),
            Right: key => key
        );

        var stream = await cache.AudioFile(format)
            .IfNoneAsync(() => StreamFromWeb(format, audoioKey, mercury, ct));

        return new WaveeTrack(
            audioStream: stream,
            title: track.Name,
            id: id,
            metadata: trackMetadata,
            duration: TimeSpan.FromMilliseconds(track.Duration)
        );
    }

    private static async Task<Stream> StreamFromWeb(
        AudioFile format,
        ReadOnlyMemory<byte> audioKey,
        ISpotifyMercuryClient mercuryClient,
        CancellationToken ct)
    {
        //storage-resolve/files/audio/interactive/{fileId}?alt=json
        var bearer = await mercuryClient.GetAccessToken(ct);
        var storageResolve = await StorageResolve(format.FileId, bearer, ct);
        if (storageResolve.Result is not StorageResolveResponse.Types.Result.Cdn)
        {
            throw new NotSupportedException("Cannot play this track for some reason.. Cdn is not available.");
        }

        var cdnUrl = storageResolve.Cdnurl.First();
        //TODO: check expiration

        const int firstChunkStart = 0;
        const int chunkSize = SpotifyDecryptedStream.ChunkSize;
        const int firstChunkEnd = firstChunkStart + chunkSize - 1;


        using var firstChunk = await HttpIO.GetWithContentRange(
            cdnUrl,
            firstChunkStart,
            firstChunkEnd,
            Option<AuthenticationHeaderValue>.None,
            HashMap<string, string>.Empty, ct);
        var firstChunkBytes = await firstChunk.Content.ReadAsByteArrayAsync(ct);
        var numberOfChunks = (int)Math.Ceiling((double)firstChunk.Content.Headers.ContentRange?.Length / chunkSize);


        var requested = new TaskCompletionSource<byte[]>[numberOfChunks];
        requested[0] = new TaskCompletionSource<byte[]>();
        requested[0].SetResult(firstChunkBytes);
        ValueTask<byte[]> GetChunkFunc(int index)
        {
            if (requested[index] is { Task.IsCompleted: true })
            {
                return new ValueTask<byte[]>(requested[index].Task.Result);
            }

            if (requested[index] is null)
            {
                var start = index * chunkSize;
                var end = start + chunkSize - 1;
                requested[index] = new TaskCompletionSource<byte[]>();
                return new ValueTask<byte[]>(HttpIO.GetWithContentRange(
                        cdnUrl,
                        start,
                        end,
                        Option<AuthenticationHeaderValue>.None,
                        HashMap<string, string>.Empty, ct)
                    .MapAsync(x => x.Content.ReadAsByteArrayAsync(ct))
                    .ContinueWith(x=> {
                        requested[index].SetResult(x.Result);
                        return x.Result;
                    }, ct));
            }
            return new ValueTask<byte[]>(requested[index].Task);
        }

        return new SpotifyDecryptedStream(GetChunkFunc,
            length: firstChunk.Content.Headers.ContentRange?.Length ?? 0,
            audioKey,
            format);
    }

    private static async Task<StorageResolveResponse> StorageResolve(ByteString file, string jwt, CancellationToken ct)
    {
        var spClient = ApResolver.SpClient.First();

        var query = $"https://{spClient}/storage-resolve/files/audio/interactive/{{0}}";

        static string ToBase16(ByteString data)
        {
            var sp = data.Span;
            var hex = new StringBuilder(sp.Length * 2);
            foreach (var b in sp)
            {
                hex.AppendFormat("{0:x2}", b);
            }

            return hex.ToString();
        }

        var finalUri = string.Format(query, ToBase16(file));

        using var resp = await HttpIO.Get(finalUri, new AuthenticationHeaderValue("Bearer", jwt),
            HashMap<string, string>.Empty, ct);
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync();
        return StorageResolveResponse.Parser.ParseFrom(stream);
    }

    private AudioFile? GetBestFormat(Track track, PreferredQualityType preferedQuality)
    {
        foreach (var file in track.File)
        {
            switch (file.Format)
            {
                case AudioFile.Types.Format.OggVorbis96:
                    if (preferedQuality is PreferredQualityType.Normal)
                        return file;
                    break;
                case AudioFile.Types.Format.OggVorbis160:
                    if (preferedQuality is PreferredQualityType.High)
                        return file;
                    break;
                case AudioFile.Types.Format.OggVorbis320:
                    if (preferedQuality is PreferredQualityType.VeryHigh)
                        return file;
                    break;
            }
        }

        //if no format is found, return the first one
        var firstOne = track.File.FirstOrDefault(c => c.Format is AudioFile.Types.Format.OggVorbis96
            or AudioFile.Types.Format.OggVorbis160 or AudioFile.Types.Format.OggVorbis320);
        if (firstOne is null)
        {
            foreach (var alternative in track.Alternative)
            {
                var altItem = GetBestFormat(alternative, preferedQuality);
                if (altItem is not null)
                {
                    return altItem;
                }
            }
        }

        return firstOne;
    }

    private bool CanPlay(Track track, string country)
    {
        //TODO:
        return true;
    }

    public void Dispose()
    {
#pragma warning disable CS8625
        _mercuryFactory = null;
        _remoteUpdates = null;
#pragma warning restore CS8625
    }
}