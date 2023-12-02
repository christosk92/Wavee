using Mediator;
using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Library;

namespace Wavee.Spotify.Application.Library.Query;

public sealed class FetchArtistCollectionQuery : IQuery<IReadOnlyCollection<SpotifyLibraryItem<SpotifyId>>>
{
    public required string User { get; init; }
}