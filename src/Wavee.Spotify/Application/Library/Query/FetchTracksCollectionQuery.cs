using Mediator;
using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Library;

namespace Wavee.Spotify.Application.Library.Query;

public sealed class FetchTracksCollectionQuery : IQuery<IReadOnlyCollection<SpotifyLibraryItem<SpotifyId>>>
{
    public required string User { get; init; }
    public  required bool WithAlbums { get; init; }
}