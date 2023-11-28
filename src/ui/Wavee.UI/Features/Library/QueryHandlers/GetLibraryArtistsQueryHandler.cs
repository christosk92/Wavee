using Mediator;
using Wavee.UI.Entities.Artist;
using Wavee.UI.Features.Library.Queries;

namespace Wavee.UI.Features.Library.QueryHandlers;

public sealed class GetLibraryArtistsQueryHandler : IQueryHandler<GetLibraryArtistsQuery, TaskCompletionSource<LibraryItems<SimpleArtistEntity>>>
{
    public ValueTask<TaskCompletionSource<LibraryItems<SimpleArtistEntity>>> Handle(GetLibraryArtistsQuery query, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}