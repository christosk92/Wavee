using System.Collections.Immutable;
using Eum.Spotify.playlist4;
using Google.Protobuf.Collections;
using Mediator;
using Wavee.Spotify.Common;
using Wavee.Spotify.Common.Contracts;
using Wavee.UI.Domain.Playlist;
using Wavee.UI.Features.Playlists.Queries;

namespace Wavee.UI.Features.Playlists.QueryHandlers;

public sealed class GetPlaylistTracksIdsQueryHandler : IQueryHandler<GetPlaylistTracksIdsQuery, IReadOnlyCollection<PlaylistTrackInfo>>
{
    private readonly ISpotifyClient _spotifyClient;

    public GetPlaylistTracksIdsQueryHandler(ISpotifyClient spotifyClient)
    {
        _spotifyClient = spotifyClient;
    }

    public async ValueTask<IReadOnlyCollection<PlaylistTrackInfo>> Handle(GetPlaylistTracksIdsQuery query, CancellationToken cancellationToken)
    {
        var selectedList = await _spotifyClient.Playlists.GetPlaylist(SpotifyId.FromUri(query.PlaylistId), cancellationToken);
        var items = selectedList.Contents.Items;
        return ParseItems(items);
    }

    private IReadOnlyCollection<PlaylistTrackInfo> ParseItems(RepeatedField<Item> items)
    {
        Span<PlaylistTrackInfo> output = new PlaylistTrackInfo[items.Count];
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            output[index] = new PlaylistTrackInfo(item.Uri)
            {
                AddedBy = item.Attributes.HasAddedBy ? item.Attributes.AddedBy : null,
                AddedAt = item.Attributes.HasTimestamp ? DateTimeOffset.FromUnixTimeMilliseconds(item.Attributes.Timestamp) : null,
                UniqueItemId = item.Attributes.HasItemId ? item.Attributes.ItemId.ToBase64() : null,
            };
        }

        return ImmutableArray.Create(output);
    }
}