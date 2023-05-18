using Eum.Spotify;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using System.Net;
using Spotify.Metadata;
using Wavee.Core.Contracts;
using Wavee.Core.Ids;
using Wavee.Spotify.Infrastructure.Cache;
using Wavee.Spotify.Infrastructure.Mercury;
using Wavee.Spotify.Infrastructure.Playback;
using Wavee.Spotify.Infrastructure.Remote.Messaging;
using Wavee.Spotify.Models.Response;
using Wavee.UI.Infrastructure.Traits;

namespace Wavee.UI.Infrastructure.Sys;

public static class Spotify<R> where R : struct, HasSpotify<R>
{
    public static Aff<R, APWelcome> Authenticate(LoginCredentials credentials, CancellationToken ct = default) =>
        from aff in default(R).SpotifyEff.MapAsync(x => x.Authenticate(credentials, ct))
        from apwelcome in default(R).SpotifyEff.Map(x => x.WelcomeMessage())
        select apwelcome.ValueUnsafe();

    public static Eff<R, Option<IObservable<SpotifyRemoteState>>> ObserveRemoteState()
        => default(R).SpotifyEff.Map(x => x.ObserveRemoteState());

    public static Aff<R, ITrack> GetTrack(AudioId audioId) =>
        from countryCode in default(R).SpotifyEff.Map(x => x.CountryCode().IfNone("US"))
        from cdnUrl in default(R).SpotifyEff.Map(x => x.CdnUrl().IfNone("https://i.scdn.co/image/{file_id}"))
        from cache in default(R).SpotifyEff
            .Map(x => x.Cache().IfNone(new SpotifyCache(Option<string>.None)))
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
}