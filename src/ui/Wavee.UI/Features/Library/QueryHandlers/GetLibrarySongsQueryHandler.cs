using System.Collections.Immutable;
using System.Numerics;
using Mediator;
using Spotify.Metadata;
using Wavee.Spotify.Common.Contracts;
using Wavee.Spotify.Domain.Library;
using Wavee.UI.Domain.Artist;
using Wavee.UI.Domain.Library;
using Wavee.UI.Domain.Playlist;
using Wavee.UI.Domain.Track;
using Wavee.UI.Features.Library.Queries;
using Wavee.UI.Features.Playlists.Services;

namespace Wavee.UI.Features.Library.QueryHandlers;

public sealed class  GetLibrarySongsQueryHandler : IQueryHandler<GetLibrarySongsQuery, LibraryItems<SimpleTrackEntity>>
{
    private readonly ISpotifyClient _spotifyClient;
    private readonly ICachedPlaylistInfoService _cachedPlaylistInfoService;

    public GetLibrarySongsQueryHandler(
        ISpotifyClient spotifyClient,
        ICachedPlaylistInfoService cachedPlaylistInfoService)
    {
        _spotifyClient = spotifyClient;
        _cachedPlaylistInfoService = cachedPlaylistInfoService;
    }

    public async ValueTask<LibraryItems<SimpleTrackEntity>> Handle(GetLibrarySongsQuery query, CancellationToken cancellationToken)
    {
        var userId = _spotifyClient.User.Result.CanonicalUsername;
        const string playlistId = "spotify:collection:{0}:songs";
        var formatted = string.Format(playlistId, userId);

        if (!_cachedPlaylistInfoService.TryGetTracks(formatted,
                null,
                out var existingtracks))
        {
            var selectedList =
                await _spotifyClient.Library.GetTracks(cancellationToken);
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
    private LibraryItems<SimpleTrackEntity> OrderFilter(LibraryItems<SimpleTrackEntity> p0, GetLibrarySongsQuery query)
    {
        static IEnumerable<LibraryItem<SimpleTrackEntity>> Filter(IReadOnlyCollection<string> querySearch,
            IEnumerable<LibraryItem<SimpleTrackEntity>> libraryItemsItems)
        {
            if (querySearch.Count is 0)
                return libraryItemsItems;

            static bool NameContains(LibraryItem<SimpleTrackEntity> x, string query)
            {
                return x.Item.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
            }

            static bool ArtistContains(LibraryItem<SimpleTrackEntity> x, string query)
            {
                return x.Item.Artists.Any(f => f.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            static bool AlbumContains(LibraryItem<SimpleTrackEntity> x, string query)
            {
                return x.Item.Album.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
            }

            return libraryItemsItems.Where(x => querySearch.Any(f => NameContains(x, f) || ArtistContains(x, f) || AlbumContains(x, f)));
        }

        static IEnumerable<LibraryItem<SimpleTrackEntity>> Order(TrackLibrarySortField sortField,
            bool descending,
            IEnumerable<LibraryItem<SimpleTrackEntity>> libraryItemsItems)
        {
            var baseSort = sortField switch
            {
                TrackLibrarySortField.Name => libraryItemsItems.OrderBy(x=> x.Item.Name),
                TrackLibrarySortField.Artist => libraryItemsItems.OrderBy(x=> x.Item.Artists.First().Name),
                TrackLibrarySortField.Album => libraryItemsItems.OrderBy(x=> x.Item.Album.Name),
                TrackLibrarySortField.Added => libraryItemsItems.OrderBy(x=> x.AddedAt),
                TrackLibrarySortField.Duration => libraryItemsItems.OrderBy(x=> x.Item.Duration),
                _ => throw new ArgumentOutOfRangeException(nameof(sortField), sortField, null)
            };
            return descending ? baseSort.Reverse() : baseSort;
        }

        return new LibraryItems<SimpleTrackEntity>
        {
            Items = Order(query.SortField, query.SortDescending, Filter(query.Search, p0.Items)).ToImmutableArray(),
            Total = p0.Total
        };
    }
    private LibraryItems<SimpleTrackEntity> ToLibraryItems(IReadOnlyCollection<SpotifyLibraryItem<Track>> items)
    {
        return new LibraryItems<SimpleTrackEntity>
        {
            Items = items.Select(f => new LibraryItem<SimpleTrackEntity>
            {
                Item = f.Item.ToSimpleTrack(),
                AddedAt = f.AddedAt,
                LastPlayedAt = null
            }).ToImmutableArray(),
            Total = items.Count
        };
    }

    internal static IReadOnlyCollection<PlaylistTrackInfo> ToPlaylistItems(LibraryItems<SimpleTrackEntity> items)
    {
        return items.Items.Select(f => new PlaylistTrackInfo
        {
            AddedBy = null,
            AddedAt = f.AddedAt,
            UniqueItemId = f.Item.Id,
            Id = f.Item.Id,
            Item = new WaveeTrackOrEpisodeOrArtist(f.Item,
                null,
                null,
                f.Item.Id),
            LastPlayedAt = null
        }).ToImmutableArray();
    }

    private LibraryItems<SimpleTrackEntity> ToLibraryItems(IReadOnlyCollection<PlaylistTrackInfo> existingtracks)
    {
        return new LibraryItems<SimpleTrackEntity>
        {
            Items = existingtracks.Select(f => new LibraryItem<SimpleTrackEntity>
            {
                Item = f.Item!.Value!.Track!,
                AddedAt = f.AddedAt ?? DateTimeOffset.MinValue,
                LastPlayedAt = null
            }).ToImmutableArray(),
            Total = existingtracks.Count
        };
    }
}