using System.Text.Json;
using Eum.Spotify;
using Eum.Spotify.playlist4;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.Attributes;
using LanguageExt.Effects.Traits;
using Spotify.Collection.Proto.V2;
using Wavee.Core.Ids;
using Wavee.Spotify.Infrastructure.Cache;
using Wavee.Spotify.Infrastructure.Mercury;
using Wavee.Spotify.Infrastructure.Mercury.Models;
using Wavee.Spotify.Infrastructure.PrivateApi.Contracts;
using Wavee.Spotify.Infrastructure.Remote.Contracts;

namespace Wavee.UI.Infrastructure.Traits;

public interface SpotifyIO
{
    ValueTask<Unit> Authenticate(LoginCredentials credentials, CancellationToken ct = default);
    Option<APWelcome> WelcomeMessage();
    Option<IObservable<SpotifyRootlistUpdateNotification>> ObserveRootlist();
    Option<IObservable<SpotifyLibraryUpdateNotification>> ObserveLibrary();
    Option<IObservable<SpotifyRemoteState>> ObserveRemoteState();
    Option<IObservable<Diff>> ObservePlaylist(AudioId id);
    Option<ISpotifyPrivateApi> PrivateApi();
    Option<ISpotifyCache> Cache();
    Option<string> CountryCode();
    ISpotifyMercuryClient Mercury();
    Option<string> GetOwnDeviceId();
    Option<ISpotifyRemoteClient> GetRemoteClient();
    Aff<SelectedListContent> GetRootList(CancellationToken ct);
    Aff<JsonDocument> FetchDesktopHome(string types,
        int limit, int offset,
        int contentLimit, int contentOffset,
        CancellationToken ct);

    Aff<T> GetFromPublicApi<T>(string endpoint, CancellationToken cancellation);
    Aff<Unit> AddToPlaylist(AudioId playlistId,
        string baseRevision,
        Seq<AudioId> audioIds, Option<int> position);
    Aff<Unit> WriteLibrary(WriteRequest writeRequest, CancellationToken ct);
    Aff<Seq<TrackOrEpisode>> FetchBatchOfTracks(Seq<AudioId> request, CancellationToken ct = default);
    Aff<SelectedListContent> FetchPlaylist(AudioId playlistId);
    Aff<Diff> DiffRevision(AudioId playlistId, ByteString currentRevision);
}

/// <summary>
/// Type-class giving a struct the trait of supporting spotify IO
/// </summary>
/// <typeparam name="RT">Runtime</typeparam>
[Typeclass("*")]
public interface HasSpotify<RT> : HasCancel<RT>
    where RT : struct, HasCancel<RT>
{
    /// <summary>
    /// Access the spotify synchronous effect environment
    /// </summary>
    /// <returns>Spotify synchronous effect environment</returns>
    Eff<RT, SpotifyIO> SpotifyEff { get; }
}
