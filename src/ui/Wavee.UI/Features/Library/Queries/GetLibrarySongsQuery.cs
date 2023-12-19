using Wavee.UI.Domain.Library;
using Wavee.UI.Domain.Track;

namespace Wavee.UI.Features.Library.Queries;

public sealed class GetLibrarySongsQuery : GetLibraryItemsQuery<SimpleTrackEntity, TrackLibrarySortField>
{

}