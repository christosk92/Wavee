using Mediator;
using Wavee.Spotify.Common.Contracts;
using Wavee.Spotify.Domain.Common;
using Wavee.Spotify.Domain.Library;
using Wavee.UI.Domain.Library;
using Wavee.UI.Entities.Artist;
using Wavee.UI.Features.Library.Queries;
using Wavee.UI.Features.Library.ViewModels.Artist;

namespace Wavee.UI.Features.Library.QueryHandlers;

public sealed class GetLibraryArtistsQueryHandler : IQueryHandler<GetLibraryArtistsQuery, LibraryItems<SimpleArtistEntity>>
{
    private readonly IMediator _mediator;
    private readonly ISpotifyClient _spotifyClient;

    public GetLibraryArtistsQueryHandler(IMediator mediator, ISpotifyClient spotifyClient)
    {
        _mediator = mediator;
        _spotifyClient = spotifyClient;
    }

    public async ValueTask<LibraryItems<SimpleArtistEntity>> Handle(GetLibraryArtistsQuery query, CancellationToken cancellationToken)
    {
        var libraryItems = await _spotifyClient.Library.GetArtists(
            query: query.Search,
            order: query.SortField switch
            {
                nameof(LibraryItem<SimpleArtistEntity>.AddedAt) => SpotifyArtistLibrarySortField.RecentlyAdded,
                nameof(LibraryItem<SimpleArtistEntity>.Item.Name) => SpotifyArtistLibrarySortField.Alphabetical,
                "Recents" => SpotifyArtistLibrarySortField.Recents,
            },
            offset: query.Offset,
            limit: query.Limit,
            cancellationToken: cancellationToken);

        return new LibraryItems<SimpleArtistEntity>
        {
            Items = libraryItems.Items.Select(x => new LibraryItem<SimpleArtistEntity>
            {
                Item = new SimpleArtistEntity
                {
                    Id = x.Item.Id.ToString(),
                    Name = x.Item.Name,
                    Images = x.Item.Images
                },
                AddedAt = x.AddedAt,
                LastPlayedAt = x.LastPlayedAt,
            }).ToArray(),
            Total = libraryItems.Total
        };

    }
}