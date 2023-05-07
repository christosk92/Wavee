using System.Runtime.CompilerServices;
using System.Text.Json;
using Eum.Spotify.context;
using Eum.Spotify.transfer;
using LanguageExt.Effects.Traits;
using LanguageExt.UnsafeValueAccess;
using Spotify.Metadata;
using Wavee.Common;
using Wavee.Infrastructure.Traits;
using Wavee.Player;
using Wavee.Player.Context;
using Wavee.Player.Playback;
using Wavee.Spotify.Sys.Common;
using Wavee.Spotify.Sys.Mercury;
using Wavee.Spotify.Sys.Remote;

[assembly: InternalsVisibleTo("Wavee.Spotify")]

namespace Wavee.Spotify.Sys.Playback;

internal class SpotifyPlaybackRuntime<RT> where RT : struct, HasHttp<RT>, HasTCP<RT>
{
    public static Aff<RT, bool> Handle(SpotifyRequestCommand request, IWaveePlayer player,
        SpotifyRemoteInfo remoteInfo,
        SpotifyConnectionInfo connectionInfo,
        SpotifyRemoteConfig config)
        => request.Endpoint switch
        {
            SpotifyRequestCommandType.Transfer => HandleTransfer(request, player, remoteInfo, connectionInfo, config),
            _ => FailEff<RT, bool>(Error.New($"Unknown endpoint: {request.Endpoint}"))
        };

    private static Aff<RT, bool> HandleTransfer(SpotifyRequestCommand request, IWaveePlayer player,
        SpotifyRemoteInfo remoteInfo,
        SpotifyConnectionInfo connectionInfo,
        SpotifyRemoteConfig config) =>
        from transferState in SuccessEff(TransferState.Parser.ParseFrom(request.Data.Span))
        from _ in ReplacePlayOrigin(transferState, remoteInfo)
        from buildContext in BuildContext(connectionInfo, transferState, config)
        from __ in BuildQueue(transferState) //todo
        from startFrom in ParseStartFrom(transferState)
        from play in ParsePlay(transferState)
        from ___ in player.Command(new PlayContextCommand(buildContext, Option<int>.None, startFrom, play)).ToAff()
        select true;

    private static Eff<RT, bool> ParsePlay(TransferState transferState)
    {
        return SuccessEff(!transferState.Playback.IsPaused);
    }

    private static Eff<RT, TimeSpan> ParseStartFrom(TransferState transferState)
    {
        var positionAsOfTimestamp = transferState.Playback.PositionAsOfTimestamp;
        var timestamp = transferState.Playback.Timestamp;
        var isPaused = transferState.Playback.IsPaused;
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        if (isPaused)
        {
            return SuccessEff(TimeSpan.FromMilliseconds(positionAsOfTimestamp));
        }

        var diff = now - timestamp;
        var position = positionAsOfTimestamp + diff;
        return SuccessEff(TimeSpan.FromMilliseconds(position));
    }

    private static Eff<RT, Unit> BuildQueue(TransferState transferState)
    {
        return SuccessEff(unit);
    }

    private static Eff<RT, IPlayContext> BuildContext(SpotifyConnectionInfo connectionInfo, TransferState transferState,
        SpotifyRemoteConfig config)
    {
        return Eff<RT, IPlayContext>((rt) => new SpotifyContext<RT>(
            transferState.CurrentSession.Context.Uri,
            transferState.CurrentSession.Context.Url,
            rt, connectionInfo, config,
            SpotifyId.FromGid(transferState.Playback.CurrentTrack.Gid, AudioItemType.Track),
            transferState.Playback.CurrentTrack.Uid
        ));
    }

    private static Eff<RT, Unit> ReplacePlayOrigin(TransferState transferState, SpotifyRemoteInfo remoteInfo)
    {
        atomic(() => remoteInfo.DeviceStateRef
            .Swap(f =>
            {
                return f.Match(
                    Some: state => state.WithPlayOrigin(transferState.CurrentSession.PlayOrigin),
                    None: () => Option<SpotifyDeviceState>.None);
            }));

        return SuccessEff(unit);
    }

    private class SpotifyContext<RT> : IPlayContext where RT : struct, HasHttp<RT>, HasTCP<RT>
    {
        private readonly string _contextUri;
        private readonly string _contextUrl;
        private readonly RT _rt;

        private readonly SpotifyConnectionInfo _connectionInfo;
        private readonly SpotifyRemoteConfig _config;

        private readonly Ref<Option<ResolvedSpotifyContext>> _resolvedContext =
            Ref(Option<ResolvedSpotifyContext>.None);

        private readonly SpotifyId _firstTrackId;
        private readonly string _firstTrackUid;

        public SpotifyContext(string contextUri, string contextUrl,
            RT rt,
            SpotifyConnectionInfo connectionInfo, SpotifyRemoteConfig config, SpotifyId firstTrackId,
            string firstTrackUid)
        {
            _contextUri = contextUri;
            _contextUrl = contextUrl;
            _connectionInfo = connectionInfo;
            _config = config;
            _firstTrackId = firstTrackId;
            _firstTrackUid = firstTrackUid;
            _rt = rt;
        }

        public async ValueTask<(IPlaybackStream Stream, int AbsoluteIndex)> GetStreamAt(Either<Shuffle, Option<int>> at)
        {
            var ctxMaybe = await ResolveContext(_connectionInfo, _contextUri, _resolvedContext).Run(_rt);
            var ctx = ctxMaybe.ThrowIfFail();
            var absoluteIndex = at.Match(
                indexMaybe => indexMaybe.Match(
                    Some: index => index,
                    None: () => -1),
                _ => (new Random()).Next(0, ctx.Count));

            var track = absoluteIndex >= 0 ? ctx.GetAt(absoluteIndex) : _firstTrackId;
            if (track.IsNone)
                throw new Exception("Track not found");

            var trackId = track.ValueUnsafe();
            var trackStreamMaybe = await SpotifyStreamRuntime.StreamAudio<RT>(_connectionInfo, trackId, _config)
                .Run(_rt);
            var trackStream = trackStreamMaybe.ThrowIfFail();
            return (trackStream, absoluteIndex is -1 ? ctx.FindIndexOf(_firstTrackUid) : absoluteIndex);
        }

        public async ValueTask<Option<int>> Count()
        {
            var ctxMaybe = await ResolveContext(_connectionInfo, _contextUri, _resolvedContext).Run(_rt);
            var ctx = ctxMaybe.ThrowIfFail();
            return ctx.Count;
        }

        private static Aff<RT, ResolvedSpotifyContext> ResolveContext(SpotifyConnectionInfo connectionInfo,
            string uri, Ref<Option<ResolvedSpotifyContext>> set,
            CancellationToken ct = default)
        {
            if (set.Value.IsSome)
                return SuccessAff(set.Value.ValueUnsafe());

            return FetchContext(connectionInfo, uri, set, ct);
        }

        private static Aff<RT, ResolvedSpotifyContext> FetchContext(
            SpotifyConnectionInfo connectionInfo,
            string uri, Ref<Option<ResolvedSpotifyContext>> set,
            CancellationToken ct = default) =>
            from get in connectionInfo.Get($"hm://context-resolve/v1/{uri}", Option<string>.None, ct).ToAff()
            from parse in ParseContext(get)
            from _ in atomic(() => set.Swap(k => parse).ToEff())
            select parse;

        private static Eff<RT, ResolvedSpotifyContext> ParseContext(MercuryResponse response) =>
            from json in Eff(() => JsonDocument.Parse(response.Body))
            from metadata in Eff(() =>
            {
                var metadata = new HashMap<string, string>();
                var metadataJson = json.RootElement.GetProperty("metadata");
                foreach (var property in metadataJson.EnumerateObject())
                {
                    metadata = metadata.Add(property.Name, property.Value.GetString());
                }

                return metadata;
            })
            from pages in Eff(() =>
            {
                var pages = new Seq<ContextPage>();
                var pagesJson = json.RootElement.GetProperty("pages");
                using var enumerator = pagesJson.EnumerateArray();
                foreach (var page in enumerator)
                {
                    var hasTracks = page.TryGetProperty("tracks", out var tracksJson);
                    var tracks = hasTracks
                        ? tracksJson.EnumerateArray().Select(t =>
                        {
                            var ctx = new ContextTrack
                            {
                                Uri = t.GetProperty("uri").GetString(),
                                Uid = t.GetProperty("uid").GetString(),
                            };
                            if (t.TryGetProperty("metadata", out var trackMetadata))
                            {
                                //enuerate metadata
                                using var metadataEnumerator = trackMetadata.EnumerateObject();
                                foreach (var property in metadataEnumerator)
                                {
                                    ctx.Metadata[property.Name] = property.Value.GetString();
                                }
                            }

                            return ctx;
                        }).ToSeq()
                        : Empty;

                    var ctxPage = new ContextPage();
                    ctxPage.Tracks.AddRange(tracks);

                    var hasPageUrl = page.TryGetProperty("pageUrl", out var pageUrlJson);
                    if (hasPageUrl)
                    {
                        ctxPage.PageUrl = pageUrlJson.GetString();
                    }

                    pages = pages.Add(ctxPage);
                }

                return pages;
            })
            select new ResolvedSpotifyContext(metadata, pages);
    }
}

internal readonly record struct ResolvedSpotifyContext(
    HashMap<string, string> Metadata,
    Seq<ContextPage> Pages)
{
    public int Count => Pages.Sum(p => p.Tracks.Count);

    public Option<SpotifyId> GetAt(int absoluteIndex)
    {
        throw new NotImplementedException();
    }

    public int FindIndexOf(string uid)
    {
        bool found = false;
        int index = 0;

        foreach (var page in Pages)
        {
            foreach (var track in page.Tracks)
            {
                if (track.Uid == uid)
                {
                    found = true;
                    break;
                }

                index++;
            }

            if (found)
                break;
        }

        return index;
    }

    public static async ValueTask<ResolvedSpotifyContext> ResolvePage(string pageUrl)
    {
        throw new NotImplementedException();
    }
}