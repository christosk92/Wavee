using Mediator;
using Wavee.UI.Domain.Album;
using Wavee.UI.Domain.Artist;

namespace Wavee.UI.Features.Artist.Queries;

public sealed class GetAlbumsForArtistQuery : IQuery<ArtistAlbumsResult>
{
    public required string Id{ get; init; }
    public required int Limit { get; init; }
    public required int Offset { get; init; }
    public DiscographyGroupType? Group { get; init; }
    public bool FetchTracks { get; init; } = true;
}

public sealed class ArtistAlbumsResult
{
    public required uint Total { get; init; }
    public required IReadOnlyCollection<SimpleAlbumEntity> Albums { get; init; }
}