using System.Collections.Immutable;
using System.Numerics;
using Mediator;
using Wavee.Spotify.Common.Contracts;
using Wavee.Spotify.Domain.Artist;
using Wavee.Spotify.Domain.Library;
using Wavee.UI.Domain.Artist;
using Wavee.UI.Domain.Library;
using Wavee.UI.Domain.Playlist;
using Wavee.UI.Domain.Track;
using Wavee.UI.Features.Library.Queries;
using Wavee.UI.Features.Playlists.Services;

namespace Wavee.UI.Features.Library.QueryHandlers;

public sealed class GetLibraryArtistsQueryHandler : IQueryHandler<GetLibraryArtistsQuery, LibraryItems<SimpleArtistEntity>>
{
    private readonly IMediator _mediator;
    private readonly ISpotifyClient _spotifyClient;
    private readonly ICachedPlaylistInfoService _cachedPlaylistInfoService;

    public GetLibraryArtistsQueryHandler(IMediator mediator, ISpotifyClient spotifyClient, ICachedPlaylistInfoService cachedPlaylistInfoService)
    {
        _mediator = mediator;
        _spotifyClient = spotifyClient;
        _cachedPlaylistInfoService = cachedPlaylistInfoService;
    }

    public async ValueTask<LibraryItems<SimpleArtistEntity>> Handle(GetLibraryArtistsQuery query, CancellationToken cancellationToken)
    {
        var userId = _spotifyClient.User.Result.CanonicalUsername;
        const string playlistId = "spotify:collection:{0}:artist";
        var formatted = string.Format(playlistId, userId);

        if (!_cachedPlaylistInfoService.TryGetTracks(formatted,
                null,
                out var existingtracks)
            || query.SortField is ArtistLibrarySortField.Recents)
        {
            var selectedList =
                await _spotifyClient.Library.GetArtists(
                    orderOnRecentlyPlayed: query.SortField is ArtistLibrarySortField.Recents,
                    cancellationToken);
            var items = selectedList.Items;

            var x = ToLibraryItems(items);
            var y = ToPlaylistItems(x);

            _cachedPlaylistInfoService.SetTracks(formatted,
                BigInteger.Zero,
                y);

            return OrderFilter(x, query);
        }

        return OrderFilter(ToLibraryItems(existingtracks), query);
    }


    private LibraryItems<SimpleArtistEntity> OrderFilter(LibraryItems<SimpleArtistEntity> p0, GetLibraryArtistsQuery query)
    {
        static IEnumerable<LibraryItem<SimpleArtistEntity>> Filter(IReadOnlyCollection<string> querySearch,
            IEnumerable<LibraryItem<SimpleArtistEntity>> libraryItemsItems)
        {
            if (querySearch.Count is 0)
                return libraryItemsItems;

            static bool NameContains(LibraryItem<SimpleArtistEntity> x, string query)
            {
                return x.Item.Name.ToLowerInvariant().Contains(query);
            }

            return libraryItemsItems.Where(x => querySearch.Any(f => NameContains(x, f)));
        }

        static IEnumerable<LibraryItem<SimpleArtistEntity>> Order(ArtistLibrarySortField sortField,
            bool sortDescending,
            IEnumerable<LibraryItem<SimpleArtistEntity>> libraryItemsItems)
        {

            var ordered = sortField switch
            {
                ArtistLibrarySortField.RecentlyAdded => libraryItemsItems.OrderBy(x => x.AddedAt),
                ArtistLibrarySortField.Alphabetical => libraryItemsItems.OrderBy(x => x.Item.Name),
                ArtistLibrarySortField.Recents => libraryItemsItems.OrderBy(x => x.LastPlayedAt),
                _ => throw new ArgumentOutOfRangeException(nameof(sortField), sortField, null)
            };
            if (sortField is ArtistLibrarySortField.Recents)
            {
                //Sorry! Only descending is supported for this field
                var reversed =  ordered.Reverse();
                return reversed;
            }
            if (sortDescending)
                return ordered.Reverse();
            return ordered;

            // return sortField switch
            // {
            //     SpotifyArtistLibrarySortField.RecentlyAdded => libraryItemsItems.OrderByDescending(x => x.AddedAt),
            //     SpotifyArtistLibrarySortField.Alphabetical => libraryItemsItems.OrderBy(x => x.Item.Name),
            //     SpotifyArtistLibrarySortField.Recents => libraryItemsItems.OrderByDescending(x => x.LastPlayedAt),
            //     _ => throw new ArgumentOutOfRangeException(nameof(sortField), sortField, null)
            // };
        }

        return new LibraryItems<SimpleArtistEntity>
        {
            Items = Order(query.SortField, query.SortDescending, Filter(query.Search, p0.Items)).ToImmutableArray(),
            Total = p0.Total
        };
    }


    private LibraryItems<SimpleArtistEntity> ToLibraryItems(IReadOnlyCollection<SpotifyLibraryItem<global::Spotify.Metadata.Artist>> items)
    {
        return new LibraryItems<SimpleArtistEntity>
        {
            Items = items.Select(f => new LibraryItem<SimpleArtistEntity>
            {
                Item = f.Item.ToSimpleArtist(),
                AddedAt = f.AddedAt,
                LastPlayedAt = f.LastPlayedAt
            }).ToImmutableArray(),
            Total = items.Count
        };
    }

    internal static IReadOnlyCollection<PlaylistTrackInfo> ToPlaylistItems(LibraryItems<SimpleArtistEntity> items)
    {
        return items.Items.Select(f => new PlaylistTrackInfo
        {
            AddedBy = null,
            AddedAt = f.AddedAt,
            UniqueItemId = f.Item.Id,
            Id = f.Item.Id,
            Item = new WaveeTrackOrEpisodeOrArtist(null,
                null,
                f.Item,
                f.Item.Id),
            LastPlayedAt = f.LastPlayedAt
        }).ToImmutableArray();
    }

    private LibraryItems<SimpleArtistEntity> ToLibraryItems(IReadOnlyCollection<PlaylistTrackInfo> existingtracks)
    {
        return new LibraryItems<SimpleArtistEntity>
        {
            Items = existingtracks.Select(f => new LibraryItem<SimpleArtistEntity>
            {
                Item = f.Item!.Value!.Artist!,
                AddedAt = f.AddedAt ?? DateTimeOffset.MinValue,
                LastPlayedAt = f.LastPlayedAt
            }).ToImmutableArray(),
            Total = existingtracks.Count
        };
    }
    // private IEnumerable<SpotifyLibraryItem<SpotifySimpleArtist>> Filter(string? querySearch, IReadOnlyCollection<SpotifyLibraryItem<global::Spotify.Metadata.Artist>> libraryItemsItems)
    // {
    //     if (string.IsNullOrWhiteSpace(querySearch))
    //         return libraryItemsItems;
    //
    //     var query = querySearch!.ToLowerInvariant();
    //     return libraryItemsItems.Where(x => x.Item.Name.ToLowerInvariant().Contains(query));
    // }
}