using Wavee.UI.Domain.Artist;

namespace Wavee.UI.Features.Library.Queries;

public sealed class GetLibraryArtistsQuery : GetLibraryItemsQuery<SimpleArtistEntity, ArtistLibrarySortField>
{
}