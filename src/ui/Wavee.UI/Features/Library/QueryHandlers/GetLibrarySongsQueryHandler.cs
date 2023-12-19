using System.Collections.Immutable;
using System.Numerics;
using Mediator;
using Wavee.Spotify.Common;
using Wavee.Spotify.Common.Contracts;
using Wavee.Spotify.Domain.Library;
using Wavee.UI.Domain.Library;
using Wavee.UI.Domain.Playlist;
using Wavee.UI.Features.Library.Queries;
using Wavee.UI.Features.Playlists.Services;

namespace Wavee.UI.Features.Library.QueryHandlers;

public sealed class GetLibrarySongsQueryHandler : IQueryHandler<GetLibrarySongsQuery, LibraryItems<string>>
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

    public async ValueTask<LibraryItems<string>> Handle(GetLibrarySongsQuery query, CancellationToken cancellationToken)
    {
        var userId = _spotifyClient.User.Result.CanonicalUsername;
        const string playlistId = "spotify:collection:{0}:songs";
        var formatted = string.Format(playlistId, userId);

        if (!_cachedPlaylistInfoService.TryGetTracks(formatted,
                null,
                out var existingtracks))
        {
            var selectedList =
                await _spotifyClient.Library.GetTrackIdsAsync(cancellationToken);
            var items = selectedList.Items;
            var x = ToLibraryItems(items);
            var y = ToPlaylistItems(items);
            _cachedPlaylistInfoService.SetTracks(formatted,
                BigInteger.Zero,
                y);
            return x;
        }

        return ToLibraryItems(existingtracks);
    }

    private LibraryItems<string> ToLibraryItems(IReadOnlyCollection<PlaylistTrackInfo> existingtracks)
    {
        return new LibraryItems<string>
        {
            Items = existingtracks.Select(f => new LibraryItem<string>
            {
                Item = f.Id,
                AddedAt = f.AddedAt ?? DateTimeOffset.MinValue,
                LastPlayedAt = null
            }).ToImmutableArray(),
            Total = existingtracks.Count
        };
    }

    internal static IReadOnlyCollection<PlaylistTrackInfo> ToPlaylistItems(IReadOnlyCollection<SpotifyLibraryItem<SpotifyId>> items)
    {
        return items.Select(f => new PlaylistTrackInfo
        {
            AddedBy = null,
            AddedAt = f.AddedAt,
            UniqueItemId = f.Item.ToString(), 
            Id = f.Item.ToString()
        }).ToImmutableArray();
    }

    private LibraryItems<string> ToLibraryItems(IReadOnlyCollection<SpotifyLibraryItem<SpotifyId>> items)
    {
        return new LibraryItems<string>
        {
            Items = items.Select(f => new LibraryItem<string>
            {
                Item = f.Item.ToString(),
                AddedAt = f.AddedAt,
                LastPlayedAt = null
            }).ToImmutableArray(),
            Total = items.Count
        };
    }
}