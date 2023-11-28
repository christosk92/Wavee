using Mediator;
using Wavee.UI.Entities.Artist;
using Wavee.UI.Features.Library.Commands;
using Wavee.UI.Features.Library.DataAcces;
using Wavee.UI.Features.Library.Queries;

namespace Wavee.UI.Features.Library.QueryHandlers;

public sealed class GetLibraryArtistsQueryHandler : IQueryHandler<GetLibraryArtistsQuery, TaskCompletionSource<LibraryItems<SimpleArtistEntity>>>
{
    private readonly IMediator _mediator;
    private readonly ILibraryRepository _libraryRepository;

    public GetLibraryArtistsQueryHandler(IMediator mediator, ILibraryRepository libraryRepository)
    {
        _mediator = mediator;
        _libraryRepository = libraryRepository;
    }

    public async ValueTask<TaskCompletionSource<LibraryItems<SimpleArtistEntity>>> Handle(GetLibraryArtistsQuery query, CancellationToken cancellationToken)
    {
        var initializeTask = await _mediator.Send(new InitializeLibraryCommand());
        if (!initializeTask.Task.IsCompleted)
        {
            var cts = new TaskCompletionSource<LibraryItems<SimpleArtistEntity>>();
            _ = Task.Run(async () =>
            {
                await initializeTask.Task.ContinueWith(async t =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        var artists =
                           await _libraryRepository.GetArtists(
                               userId: string.Empty,
                                query.Search,
                                offset: query.Offset,
                                limit: query.Limit,
                                sortField: query.SortField,
                                sortDirection: query.SortDirection);
                        cts.SetResult(artists);
                    }
                    else
                    {
                        cts.SetException(t.Exception);
                    }
                });
            }, cancellationToken);

            return cts;
        }

        var tcs = new TaskCompletionSource<LibraryItems<SimpleArtistEntity>>();
        var artists =
            await _libraryRepository.GetArtists(
                userId: string.Empty,
                query.Search,
                offset: query.Offset,
                limit: query.Limit,
                sortField: query.SortField,
                sortDirection: query.SortDirection);
        tcs.SetResult(artists);
        return tcs;
    }
}