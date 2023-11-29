using Mediator;
using Wavee.UI.Domain.Artist;

namespace Wavee.UI.Features.Artist.Queries;

public sealed class GetAlbumsForArtistQuery : IQuery<ArtistAlbumsResult>
{
    public required string Id{ get; init; }
    public required uint Limit { get; init; }
    public required uint Offset { get; init; }
}

public sealed class ArtistAlbumsResult
{
    public required uint Total { get; init; }
    public required IReadOnlyCollection<ArtistAlbumEntity> Albums { get; init; }
}