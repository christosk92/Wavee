using System.Reactive.Linq;
using System.Text.Json;
using Eum.Spotify.connectstate;
using Eum.Spotify.context;
using Google.Protobuf;
using LanguageExt.UnsafeValueAccess;
using Spotify.Metadata;
using Wavee.Infrastructure.Traits;
using Wavee.Player;
using Wavee.Player.States;
using Wavee.Spotify.Cache.Repositories;
using Wavee.Spotify.Clients.Info;
using Wavee.Spotify.Clients.Mercury;
using Wavee.Spotify.Clients.Mercury.Key;
using Wavee.Spotify.Clients.Mercury.Metadata;
using Wavee.Spotify.Clients.Playback.Streams;
using Wavee.Spotify.Configs;
using Wavee.Spotify.Id;
using Wavee.Spotify.Infrastructure.Sys;

namespace Wavee.Spotify.Clients.Playback;

internal readonly struct PlaybackClient<RT> : IPlaybackClient
    where RT : struct, HasLog<RT>, HasHttp<RT>, HasAudioOutput<RT>, HasTrackRepo<RT>, HasFileRepo<RT>
{
    private static AtomHashMap<Guid, Action<SpotifyPlaybackInfo>> _onPlaybackInfo =
        LanguageExt.AtomHashMap<Guid, Action<SpotifyPlaybackInfo>>.Empty;

    private readonly Guid _mainConnectionId;

    private readonly Func<ValueTask<string>> _getBearer;

    private readonly Func<SpotifyId, ByteString, CancellationToken, Aff<RT, Either<AesKeyError, AudioKey>>>
        _fetchAudioKeyFunc;

    private readonly IMercuryClient _mercury;
    private readonly RT _runtime;
    private readonly PreferredQualityType _preferredQuality;
    private readonly bool _autoplay;

    public PlaybackClient(Guid mainConnectionId,
        Func<ValueTask<string>> getBearer,
        Func<SpotifyId, ByteString, CancellationToken, Aff<RT, Either<AesKeyError, AudioKey>>> fetchAudioKeyFunc,
        IMercuryClient mercury, RT runtime,
        Action<SpotifyPlaybackInfo> onPlaybackInfo,
        PreferredQualityType preferredQuality, bool autoplay)
    {
        _mainConnectionId = mainConnectionId;
        _getBearer = getBearer;
        _fetchAudioKeyFunc = fetchAudioKeyFunc;
        _mercury = mercury;
        _runtime = runtime;
        _preferredQuality = preferredQuality;
        _autoplay = autoplay;
        _onPlaybackInfo.AddOrUpdate(mainConnectionId, onPlaybackInfo);
    }


    public Guid Listen(Action<SpotifyPlaybackInfo> onPlaybackInfo)
    {
        var g = Guid.NewGuid();
        _onPlaybackInfo.AddOrUpdate(g, onPlaybackInfo);
        return g;
    }


    public async Task<SpotifyPlaybackInfo> PlayTrack(string uri,
        Option<PreferredQualityType> preferredQualityOverride,
        CancellationToken ct = default)
    {
        var baseInfo = new SpotifyPlaybackInfo(Option<ProvidedTrack>.None,
            None,
            uri,
            Option<string>.None,
            LanguageExt.HashMap<string, Seq<string>>.Empty,
            None,
            Empty,
            None, None, false, true);

        _onPlaybackInfo.Iter(x => x(baseInfo));

        await Play(baseInfo,
            SpotifyId.FromUri(uri),
            uri,
            _autoplay,
            0,
            i => (None, None),
            _mercury,
            ct
        );

        return baseInfo;
    }

    public async Task<SpotifyPlaybackInfo> PlayContext(string uri, int startFrom, CancellationToken ct = default)
    {
        var baseInfo = new SpotifyPlaybackInfo(
            Option<ProvidedTrack>.None,
            None,
            uri,
            None,
            Empty,
            Option<int>.None,
            Empty,
            None, None, false, true);

        _onPlaybackInfo.Iter(x => x(baseInfo));

        var ctx = await _mercury.ContextResolve(uri, ct);
        Option<ContextTrack> track = None;
        Option<int> index = None;
        while (track.IsNone)
        {
            if (ctx.Pages.Count == 0)
                throw new Exception("ContextResolve returned empty pages");

            //so we want to start at index 30 for example
            //but the first page only has 20 items
            //so we need to skip the first page
            int depth = 0;
            var totalTracks = ctx.Pages.Sum(x => x.Tracks.Count);
            if (startFrom < totalTracks)
            {
                foreach (var page in ctx.Pages)
                {
                    //first lets check if we have the track in any of the pages
                    //check if index is in range
                    if (startFrom < page.Tracks.Count)
                    {
                        var item = page.Tracks[startFrom - depth];
                        track = Some(item);
                        index = Some(startFrom);
                        break;
                    }

                    //lets check if we we have a next page
                    if (page.HasNextPageUrl)
                    {
                        //we need to load the next page
                        var nextPage = await _mercury.Get(page.NextPageUrl, ct);

                        //TODO:
                    }

                    //we need to skip this page
                    depth += page.Tracks.Count;
                    continue;
                }
            }
            else
            {
                //lets check if we have more pages
                foreach (var page in ctx.Pages)
                {
                    if (page.HasNextPageUrl)
                    {
                        //we need to load the next page
                        var nextPage = await _mercury.Get(page.NextPageUrl, ct);
                    }
                    else if (page.HasPageUrl)
                    {
                        var nextPage = await _mercury.Get(page.PageUrl, ct);
                        using var doc = JsonDocument.Parse(nextPage.Body);
                        var parsedCtxPage = ContextHelper.ParsePage(doc.RootElement);
                        page.NextPageUrl = parsedCtxPage.NextPageUrl;
                        page.Tracks.AddRange(parsedCtxPage.Tracks);
                        page.PageUrl = string.Empty;
                        if (startFrom < ctx.Pages.Sum(x => x.Tracks.Count))
                        {
                            //we have the track
                            var item = ctx.Pages.SelectMany(x => x.Tracks).ElementAt(startFrom);
                            track = Some(item);
                            index = Some(startFrom);
                            break;
                        }
                        //we need to load the next page
                    }
                }
            }

            //we need to load the next page
        }

        var tr = track.ValueUnsafe();

        _onPlaybackInfo.Iter(x =>
        {
            baseInfo = baseInfo
                .EnrichContext(
                    contextUri: uri,
                    contextUrl: ctx.Url,
                    track.ValueUnsafe(),
                    ctx.Metadata,
                    ctx.Restrictions)
                .EnrichIndex(index)
                .WithBuffering();
            x(baseInfo);
        });

        await Play(baseInfo,
            tr.HasUri ? SpotifyId.FromUri(tr.Uri) : SpotifyId.FromRaw(tr.Gid.Span, AudioItemType.Track),
            uri,
            _autoplay,
            startFrom,
            i =>
            {
                var theoreticalNext = i + 1;
                var nextTrack = ctx.Pages.SelectMany(x => x.Tracks).ElementAtOrDefault(theoreticalNext);
                if (nextTrack != null)
                {
                    _onPlaybackInfo.Iter(x =>
                    {
                        baseInfo = baseInfo
                            .EnrichContext(
                                contextUri: uri,
                                contextUrl: ctx.Url,
                                nextTrack,
                                ctx.Metadata,
                                ctx.Restrictions)
                            .EnrichIndex(theoreticalNext)
                            .WithBuffering();
                        x(baseInfo);
                    });
                    return (Some(theoreticalNext), Some(
                        nextTrack.HasUri
                            ? SpotifyId.FromUri(nextTrack.Uri)
                            : SpotifyId.FromRaw(nextTrack.Gid.Span, AudioItemType.Track)));
                }

                return (None, None);
            },
            _mercury, ct);


        return baseInfo;
    }

    private async Task<Unit> Play(
        SpotifyPlaybackInfo baseInfo,
        SpotifyId id,
        string context,
        bool autoplay,
        int startFrom,
        Func<int, (Option<int>, Option<SpotifyId>)> onTrackPlayed,
        IMercuryClient mercury,
        CancellationToken ct = default)
    {
        var trackStreamAff = await SpotifyPlayback<RT>.LoadTrack(
            id,
            _preferredQuality,
            _getBearer,
            _fetchAudioKeyFunc,
            _mercury, ct).Run(_runtime);

        var stream = trackStreamAff.ThrowIfFail();

        _onPlaybackInfo.Iter(x =>
        {
            baseInfo = baseInfo
                .EnrichFrom(stream.Metadata)
                .WithBuffering();
            x(baseInfo);
        });

        Option<string> playbackId = None;
        IDisposable listener = default;
        PlaybackClient<RT> tmpThis = this;
        listener = WaveePlayer.Instance.StateObservable.Select(async state =>
        {
            if (state is WaveeEndOfTrackState p)
            {
                if (
                    playbackId.IsSome &&
                    baseInfo.PlaybackId.IsSome &&
                    p.PlaybackId == baseInfo.PlaybackId.ValueUnsafe())
                {
                    listener?.Dispose();
                }
            }

            switch (state)
            {
                case WaveeEndOfTrackState eot:
                    if (!eot.GoingToNextTrackAlready)
                    {
                        //start loading next track
                        _onPlaybackInfo.Iter(x =>
                        {
                            baseInfo = baseInfo
                                .MaybeNewPlaybackId(playbackId)
                                .WithPosition(eot.Position)
                                .WithPaused(true);
                            x(baseInfo);
                        });


                        var (nextIndex, nextTrack) = onTrackPlayed(startFrom);
                        if (nextIndex.IsNone)
                        {
                            var autoPlayContext = await mercury.AutoplayQuery(context, ct);
                            await tmpThis.PlayContext(autoPlayContext, 0, CancellationToken.None);
                        }
                        else
                        {
                            var nextTrackId = nextTrack.ValueUnsafe();
                            await tmpThis.Play(
                                baseInfo,
                                nextTrackId,
                                context,
                                autoplay,
                                nextIndex.ValueUnsafe(),
                                onTrackPlayed,
                                mercury, ct);
                        }
                    }

                    break;
                case WaveePlayingState playing:
                    _onPlaybackInfo.Iter(x =>
                    {
                        baseInfo = baseInfo
                            .MaybeNewPlaybackId(playbackId)
                            .WithPosition(playing.Position)
                            .WithPaused(false);
                        x(baseInfo);
                    });
                    break;
                case WaveePausedState paused:
                    _onPlaybackInfo.Iter(x =>
                    {
                        baseInfo = baseInfo
                            .MaybeNewPlaybackId(playbackId)
                            .WithPosition(paused.Position)
                            .WithPaused(true);
                        x(baseInfo);
                    });

                    break;
            }
        }).Subscribe();
        playbackId = await WaveePlayer.Instance.Play(stream);
        return unit;
    }

    public async Task<bool> Pause(CancellationToken ct = default)
    {
        return await WaveePlayer.Instance.Pause();
    }

    public async Task<bool> Seek(TimeSpan to, CancellationToken ct = default)
    {
        return await WaveePlayer.Instance.Seek(to);
    }
}

public readonly record struct SpotifyPlaybackInfo(
    Option<ProvidedTrack> Track,
    Option<int> Duration,
    Option<string> ContextUri,
    Option<string> ContextUrl,
    HashMap<string, Seq<string>> ContextRestrictions,
    Option<int> Index,
    HashMap<string, string> ContextMetadata,
    Option<string> PlaybackId,
    Option<TimeSpan> Position,
    bool Paused,
    bool Buffering)
{
    public SpotifyPlaybackInfo WithPosition(TimeSpan playingPosition)
    {
        return this with
        {
            Position = playingPosition
        };
    }

    public SpotifyPlaybackInfo WithPaused(bool b)
    {
        return this with
        {
            Paused = b,
            Buffering = false
        };
    }

    public SpotifyPlaybackInfo MaybeNewPlaybackId(Option<string> playbackId)
    {
        return this with
        {
            PlaybackId = playbackId
        };
    }

    public SpotifyPlaybackInfo WithBuffering()
    {
        return this with
        {
            Buffering = true,
        };
    }

    public SpotifyPlaybackInfo EnrichIndex(Option<int> index)
    {
        return this with
        {
            Index = index
        };
    }

    public SpotifyPlaybackInfo EnrichContext(
        string contextUri,
        string contextUrl,
        ContextTrack ctxTrack,
        HashMap<string, string> ctxMetadata,
        HashMap<string, Seq<string>> contextRestrictions)
    {
        var track = Track.IfNone(new ProvidedTrack());
        track.Provider = "context";
        // ContextUri = contextUri;
        if (ctxTrack.HasUri)
            track.Uri = ctxTrack.Uri;
        if (ctxTrack.HasUid)
            track.Uid = ctxTrack.Uid;

        return this with
        {
            Track = track,
            ContextUrl = contextUrl,
            ContextRestrictions = contextRestrictions,
            ContextUri = contextUri,
            ContextMetadata = ctxMetadata
        };
    }

    public SpotifyPlaybackInfo EnrichFrom(TrackOrEpisode streamMetadata)
    {
        var track = Track.IfNone(new ProvidedTrack());

        return this with
        {
            Track = streamMetadata
                .Value.Match(
                    Left: e => EnrichFromEpisode(e, track),
                    Right: t => EnrichFromTrack(t, track)
                ),
            Duration = streamMetadata.Duration
        };
    }

    private static ProvidedTrack EnrichFromTrack(Track track, ProvidedTrack providedTrack)
    {
        if (track.HasPopularity)
            providedTrack.Metadata["popularity"] = track.Popularity.ToString();
        if (track.HasExplicit)
            providedTrack.Metadata["explicit"] = track.Explicit.ToString();
        if (track.HasHasLyrics)
            providedTrack.Metadata["has_lyrics"] = track.HasLyrics.ToString();
        if (track.HasName)
            providedTrack.Metadata["title"] = track.Name;
        if (track.HasDiscNumber)
            providedTrack.Metadata["album_disc_number"] = track.DiscNumber.ToString();

        for (int i = 0; i < track.Artist.Count; i++)
        {
            var artist = track.Artist[i];
            if (artist.HasName)
            {
                providedTrack.Metadata["atist_name" + (i == 0 ? string.Empty : $":{i}")] = artist.Name;
            }

            if (artist.HasGid)
            {
                providedTrack.Metadata["artist_uri" + (i == 0 ? string.Empty : $":{i}")] =
                    SpotifyId.FromRaw(artist.Gid.Span, AudioItemType.Artist).ToUri();
            }
        }

        var album = track.Album;
        if (album.Disc.Count > 0)
        {
            providedTrack.Metadata["album_track_count"] = album.Disc.Sum(x => x.Track.Count).ToString();
            providedTrack.Metadata["album_disc_count"] = album.Disc.Count.ToString();
        }

        if (album.HasName)
            providedTrack.Metadata["album_title"] = album.Name;
        if (album.HasGid)
            providedTrack.Metadata["album_uri"] = SpotifyId.FromRaw(album.Gid.Span, AudioItemType.Album).ToUri();

        for (int i = 0; i < album.Artist.Count; i++)
        {
            var artist = album.Artist[i];
            if (artist.HasName)
            {
                providedTrack.Metadata["album_atist_name" + (i == 0 ? string.Empty : $":{i}")] = artist.Name;
            }

            if (artist.HasGid)
            {
                providedTrack.Metadata["album_artist_uri" + (i == 0 ? string.Empty : $":{i}")] =
                    SpotifyId.FromRaw(artist.Gid.Span, AudioItemType.Artist).ToUri();
            }
        }

        if (track.HasDiscNumber)
        {
            foreach (var disc in album.Disc)
            {
                if (disc.Number != track.DiscNumber) continue;

                for (int i = 0; i < disc.Track.Count; i++)
                {
                    if (disc.Track[i].Gid.Span.SequenceEqual(track.Gid.Span))
                    {
                        providedTrack.Metadata["album_track_number"] = (i + 1).ToString();
                        break;
                    }
                }
            }
        }

        if (album.CoverGroup is not null)
        {
            ImageId.PutAsMetadata(providedTrack, album.CoverGroup);
        }

        providedTrack.Uri = SpotifyId.FromRaw(track.Gid.Span, AudioItemType.Track).ToUri();
        return providedTrack;
    }

    private static ProvidedTrack EnrichFromEpisode(Episode episode, ProvidedTrack track)
    {
        throw new NotImplementedException();
    }
}