using System.Numerics;
using System.Text;
using Google.Protobuf;
using Wavee.Core.Contracts;
using Wavee.Core.Enums;
using Wavee.Core.Id;
using Wavee.Core.Infrastructure.Traits;
using Wavee.Player;
using Wavee.Spotify.Cache;
using Wavee.Spotify.Clients.Mercury;
using Wavee.Spotify.Infrastructure.Sys;
using Wavee.Spotify.Models.Responses;
using Wavee.Spotify.Playback.Infrastructure.Sys;
using Wavee.Spotify.Playback.Metadata;
using Wavee.Spotify.Remote.Infrastructure;

namespace Wavee.Spotify.Infrastructure.Playback;

internal class SpotifyPlaybackClient<R> : ISpotifyPlaybackClient
    where R : struct, HasAudioOutput<R>, HasWebsocket<R>, HasLog<R>, HasHttp<R>,  HasDatabase<R>
{
    private readonly SpotifyConnection<R> _connection;
    private readonly SpotifyRemoteConnection<R> _remoteConnection;
    private readonly R _runtime;
    private readonly PreferredQualityType _preferredQualityType;
    public SpotifyPlaybackClient(SpotifyConnection<R> connection,
        SpotifyRemoteConnection<R> remoteConnection, R runtime, PreferredQualityType preferredQualityType)
    {
        _connection = connection;
        _remoteConnection = remoteConnection;
        _runtime = runtime;
        _preferredQualityType = preferredQualityType;
    }

    private static AudioId Parse(string uri)
    {
        ReadOnlySpan<string> contextidParts = uri.Split(':');
        return new AudioId(contextidParts[2], contextidParts[1] switch
        {
            "track" => AudioItemType.Track,
            "episode" => AudioItemType.PodcastEpisode,
            "playlist" => AudioItemType.Playlist,
            "album" => AudioItemType.Album,
        }, ISpotifyCore.SourceId);
    }

    public async Task PlayContext(
        string contextUri,
        int indexInContext,
        TimeSpan position,
        bool startPlaying,
        Option<PreferredQualityType> preferredQualityTypeOverride,
        CancellationToken ct = default)
    {
        var preferredQualityType = preferredQualityTypeOverride.Match(x => x, () => _preferredQualityType);
        var contextResolve = await _connection.Mercury.ContextResolve(contextUri, ct);
        var tracks = GetTracks(_connection,
            preferredQualityType,
            contextResolve, _runtime);
        var ctx = new WaveeContext(Option<IShuffleProvider>.None,
            Context: Parse(contextUri),
            Name: contextResolve.Metadata.Find("context_description").IfNone(string.Empty),
            FutureTracks: tracks
        );

        //build an ienumerable lazy list of tracks
        WaveePlayer.PlayContext(ctx, position, indexInContext, !startPlaying);
    }

    private static IEnumerable<FutureTrack> GetTracks(
        SpotifyConnection<R> connection,
        PreferredQualityType preferredQualityType,
        SpotifyContext contextResolve,
        R runtime)
    {
        foreach (var page in contextResolve.Pages)
        {
            //check if the page has tracks
            //if it does, yield return each track
            //if it doesn't, fetch the next page (if next page is set). if not go to the next page
            if (page.Tracks.Count > 0)
            {
                foreach (var track in page.Tracks)
                {
                    ReadOnlySpan<string> trackid = track.Uri.Split(':');
                    var id = new AudioId(trackid[2], trackid[1] switch
                    {
                        "track" => AudioItemType.Track,
                        "episode" => AudioItemType.PodcastEpisode,
                    }, ISpotifyCore.SourceId);
                    yield return new FutureTrack(id,
                        () => StreamFuture(connection, id, preferredQualityType, track.HasUid ? track.Uid : None, runtime));
                }
            }
            else
            {
                //fetch the page if page url is set
                //if not, go to the next page
                if (page.HasPageUrl)
                {
                    var pageUrl = page.PageUrl;
                    var pageResolve = connection.Mercury.ContextResolve(pageUrl).ConfigureAwait(false)
                        .GetAwaiter().GetResult();
                    foreach (var track in GetTracks(connection, preferredQualityType, pageResolve, runtime))
                    {
                        yield return track;
                    }
                }
                else if (page.HasNextPageUrl)
                {
                    var pageUrl = page.NextPageUrl;
                    var pageResolve = connection.Mercury.ContextResolve(pageUrl).ConfigureAwait(false)
                        .GetAwaiter().GetResult();
                    foreach (var track in GetTracks(connection, preferredQualityType, pageResolve, runtime))
                    {
                        yield return track;
                    }
                }
            }
        }
    }


    private static async Task<IAudioStream> StreamFuture(SpotifyConnection<R> connection, AudioId id,
        PreferredQualityType preferredQuality,
        Option<string> trackUid,
        R runtime)
    {
        var countryMaybe = await connection.Info.CountryCode;
        var productInfoMaybe = await connection.Info.ProductInfo;
        var cdnUrl = productInfoMaybe.Match(x => x["image_url"], () => "https://i.scdn.co/image/{image_id}");
        var countryCode = countryMaybe.IfNone("US");
        var track = await connection.Mercury.GetTrack(id);

        static ITrack Mapper(TrackOrEpisode mp, string countrycode, string cdnurl) => mp.Value
            .Match(Left: e => throw new NotImplementedException(), Right: tr => SpotifyTrackResponse.From(countrycode,
                cdnurl,
                tr
            ));

        var mapper = (TrackOrEpisode p) => Mapper(p, countryCode, cdnUrl);
        var fetchAudioKeyFunc = connection.FetchAudioKeyFunc;
        var getBearer = () => connection.Token.GetToken();
        var aff =
            from sp in AP<R>.FetchSpClient().Map(x => $"https://{x.Host}:{x.Port}")
            from stream in SpotifyPlaybackRuntime<R>.LoadTrack(sp, new TrackOrEpisode(track), trackUid, mapper,
                preferredQuality,
                getBearer,
                fetchAudioKeyFunc,
                CancellationToken.None)
            select stream;

        var affResult = (await aff.Run(runtime));
        return affResult.ThrowIfFail();
    }


    public Task PlayTrack(
        AudioId id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}