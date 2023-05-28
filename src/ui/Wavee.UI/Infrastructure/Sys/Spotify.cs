using Eum.Spotify;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using System.Net;
using Spotify.Metadata;
using Wavee.Core.Contracts;
using Wavee.Core.Ids;
using Wavee.Spotify;
using Wavee.Spotify.Infrastructure.Cache;
using Wavee.Spotify.Infrastructure.Mercury;
using Wavee.Spotify.Infrastructure.Playback;
using Wavee.Spotify.Infrastructure.Remote;
using Wavee.Spotify.Infrastructure.Remote.Messaging;
using Wavee.Spotify.Models.Response;
using Wavee.UI.Infrastructure.Traits;
using System;
using System.Text.Json;
using Eum.Spotify.playlist4;
using LanguageExt.Common;
using Spotify.Collection.Proto.V2;

namespace Wavee.UI.Infrastructure.Sys;

public static class Spotify<R> where R : struct, HasSpotify<R>
{
    public static Aff<R, APWelcome> Authenticate(LoginCredentials credentials, CancellationToken ct = default) =>
        from aff in default(R).SpotifyEff.MapAsync(x => x.Authenticate(credentials, ct))
        from apwelcome in default(R).SpotifyEff.Map(x => x.WelcomeMessage())
        select apwelcome.ValueUnsafe();

    public static Aff<R, Unit> RefreshRemoteState() =>
        from aff in default(R).SpotifyEff.Map(x => x.GetRemoteClient())
        from result in aff.Match(
            Some: x => x.RefreshState().ToAff(),
            None: () => SuccessAff(unit))
        select unit;

    public static Aff<R, T> GetFromPublicApi<T>(string endpoint, CancellationToken cancellation) =>
        from aff in default(R).SpotifyEff.Map(x => x.GetFromPublicApi<T>(endpoint, cancellation))
        from result in aff
        select result;
    public static Aff<R, SelectedListContent> GetRootList(CancellationToken ct = default) =>
        from mainAff in default(R).SpotifyEff.Map(x => x.GetRootList(ct))
        from result in mainAff
        select result;

    public static Aff<R, Unit> WriteLibrary(WriteRequest writeRequest, CancellationToken ct = default) =>
        from aff in default(R).SpotifyEff.Map(x => x.WriteLibrary(writeRequest, ct))
        from result in aff
        select unit;
    public static Aff<R, JsonDocument> FetchDesktopHome(string types, CancellationToken ct = default) =>
        from aff in default(R).SpotifyEff.Map(x => x.FetchDesktopHome(types, 20, 0,
            10, 0,
            ct))
        from result in aff
        select result;

    public static Eff<R, Option<IObservable<SpotifyRemoteState>>> ObserveRemoteState()
        => default(R).SpotifyEff.Map(x => x.ObserveRemoteState());

    public static Eff<R, Option<IObservable<SpotifyRootlistUpdateNotification>>> ObserveRootlist()
        => default(R).SpotifyEff.Map(x => x.ObserveRootlist());
    public static Eff<R, Option<IObservable<SpotifyLibraryUpdateNotification>>> ObserveLibrary()
        => default(R).SpotifyEff.Map(x => x.ObserveLibrary());

    public static Eff<R, Option<IObservable<Diff>>> ObservePlaylist(AudioId id) =>
        default(R).SpotifyEff.Map(x => x.ObservePlaylist(id));
    public static Eff<R, Option<string>> GetOwnDeviceId() =>
        default(R).SpotifyEff.Map(x => x.GetOwnDeviceId());
    public static Eff<R, SpotifyCache> Cache() => default(R).SpotifyEff.Map(x => x.Cache().
        IfNone(new SpotifyCache(new SpotifyCacheConfig(Option<string>.None,
            Option<string>.None,
            Option<TimeSpan>.None), "en")));
    public static Eff<R, Option<SpotifyRemoteClient>> GetRemoteClient() =>
        default(R).SpotifyEff.Map(x => x.GetRemoteClient());
    public static Aff<R, ITrack> GetTrack(AudioId audioId) =>
        from countryCode in default(R).SpotifyEff.Map(x => x.CountryCode().IfNone("US"))
        from cdnUrl in default(R).SpotifyEff.Map(x => x.CdnUrl().IfNone("https://i.scdn.co/image/{file_id}"))
        from cache in default(R).SpotifyEff
            .Map(x => x.Cache().IfNone(new SpotifyCache(new SpotifyCacheConfig(Option<string>.None,
                Option<string>.None,
                Option<TimeSpan>.None), "en")))
        from trackOrEpisode in cache.Get(audioId).Map(x => SuccessAff<R, TrackOrEpisode>(x))
            .IfNone(() =>
            {
                return
                    from fetchedTrack in FetchTrack(audioId, countryCode)
                    from _ in Eff(() => cache.Save(fetchedTrack))
                    select fetchedTrack;
            })
        let trackResponse = trackOrEpisode.Value.Match(
            Left: episode => default(ITrack),
            Right: track => SpotifyTrackResponse.From(countryCode, cdnUrl, track))
        select trackResponse;

    private static Aff<R, TrackOrEpisode> FetchTrack(AudioId id, string country) =>
        from mercury in default(R).SpotifyEff.Map(x => x.Mercury())
        from trackOrEpisode in mercury.GetMetadata(id, country, CancellationToken.None).ToAff()
        select trackOrEpisode;

    public static Eff<R, MercuryClient> Mercury() =>
        default(R).SpotifyEff.Map(x => x.Mercury());

    public static Aff<R, Option<string>> CountryCode() =>
         default(R).SpotifyEff.Map(x => x.CountryCode());

    public static Aff<R, Unit> AddToPlaylist(AudioId playlistId,
        string baseRevision,
        Seq<AudioId> audioIds, Option<int> position) =>
        from aff in default(R).SpotifyEff.Map(x => x.AddToPlaylist(playlistId,
            baseRevision,
            audioIds, position))
        from result in aff
        select result;

    public static Aff<R, Seq<TrackOrEpisode>> FetchBatchOfTracks(Seq<AudioId> request, CancellationToken ct = default) =>
        from cache in default(R).SpotifyEff
            .Map(x => x.Cache().IfNone(new SpotifyCache(new SpotifyCacheConfig(Option<string>.None,
                Option<string>.None,
                Option<TimeSpan>.None), "en")))
            //check which tracks are already cached
        let cachedTracks = cache.GetBulk(request).Somes()
        //fetch the rest
        from aff in default(R).SpotifyEff.Map(x => x.FetchBatchOfTracks(request.Filter(y => !cachedTracks.Any(z => z.Id == y)), ct))
        from result in aff
            //save the fetched tracks
        from _ in Eff(() => cache.SaveBulk(result.Distinct(x => x.Id)))
            //combine the cached and fetched tracks
        select cachedTracks.Append(result);

    public static Eff<R, Option<string>> CdnUrl() =>
        from aff in default(R).SpotifyEff.Map(x => x.CdnUrl())
        select aff;

    public static Aff<R, (SelectedListContent Playlist, bool FromCache)> GetPlaylistMaybeCached(AudioId playlistId) =>
        from cache in default(R).SpotifyEff
            .Map(x => x.Cache().IfNone(new SpotifyCache(new SpotifyCacheConfig(Option<string>.None,
                Option<string>.None,
                Option<TimeSpan>.None), "en")))
        from playlist in cache.GetPlaylist(playlistId).Map(x => SuccessAff<R, (SelectedListContent, bool)>((x, true)))
            .IfNone(() =>
            {
                return
                    from fetchedPlaylist in GetPlaylistFromApi(playlistId)
                    from _ in Eff(() => cache.SavePlaylist(playlistId, fetchedPlaylist))
                    select (fetchedPlaylist, false);
            })
        select playlist;

    private static Aff<R, SelectedListContent> GetPlaylistFromApi(AudioId playlistId) =>
        from affR in default(R).SpotifyEff.Map(x => x.FetchPlaylist(playlistId))
        from result in affR
        select result;

}