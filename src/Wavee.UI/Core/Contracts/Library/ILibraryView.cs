using LanguageExt;
using Wavee.Core.Ids;
using Wavee.Spotify.Infrastructure.Remote.Contracts;

namespace Wavee.UI.Core.Contracts.Library;

public interface ILibraryView
{
    Task<Unit> SaveItem(Seq<AudioId> ids, bool add, CancellationToken ct = default);
    ValueTask<Seq<SpotifyLibaryItem>> FetchTracksAndAlbums(CancellationToken ct = default);
    ValueTask<Seq<SpotifyLibaryItem>> FetchArtists(CancellationToken ct = default);
    IObservable<SpotifyLibraryUpdateNotification> ListenForChanges { get; }
}

public readonly record struct SpotifyLibaryItem(AudioId Id, DateTimeOffset AddedAt);