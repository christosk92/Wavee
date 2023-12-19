using System.Collections.Immutable;
using Eum.Spotify.playlist4;
using Google.Protobuf.Collections;
using LanguageExt;
using Mediator;
using Spotify.Metadata;
using Wavee.Spotify.Application.Metadata.Query;
using Wavee.Spotify.Common;
using Wavee.Spotify.Common.Contracts;
using Wavee.UI.Domain.Playlist;
using Wavee.UI.Domain.Podcast;
using Wavee.UI.Domain.Track;
using Wavee.UI.Extensions;
using Wavee.UI.Features.Navigation;
using Wavee.UI.Features.Playlists.Queries;
using Wavee.UI.Features.Playlists.Services;

namespace Wavee.UI.Features.Playlists.QueryHandlers;

public sealed class GetPlaylistTracksIdsQueryHandler :
    IQueryHandler<GetPlaylistTracksIdsQuery, IReadOnlyCollection<PlaylistTrackInfo>>
{
    private readonly ISpotifyClient _spotifyClient;
    private readonly ICachedPlaylistInfoService _cachedPlaylistInfoService;

    public GetPlaylistTracksIdsQueryHandler(
        ISpotifyClient spotifyClient,
        ICachedPlaylistInfoService cachedPlaylistInfoService)
    {
        _spotifyClient = spotifyClient;
        _cachedPlaylistInfoService = cachedPlaylistInfoService;
    }

    public async ValueTask<IReadOnlyCollection<PlaylistTrackInfo>> Handle(GetPlaylistTracksIdsQuery query, CancellationToken cancellationToken)
    {
        if (!_cachedPlaylistInfoService.
                TryGetTracks(query.PlaylistId,
                null,
                out var existingtracks)
            || existingtracks.Any(f=> f.Item is null))
        {
            var (selectedList, tracks)=
                await _spotifyClient.Playlists.GetPlaylistWithTracks(SpotifyId.FromUri(query.PlaylistId), cancellationToken);

            var items = selectedList.Contents.Items;
            var x = ParseItems(items, tracks.Select(f => f.MapToSimpleEntity())
                .ToDictionary(x => x.Id, x => x));

            _cachedPlaylistInfoService.SetTracks(query.PlaylistId,
                selectedList.Revision.ToBigInteger(),
                x);
            return x;
        }

        return existingtracks;
    }

    internal static IReadOnlyCollection<PlaylistTrackInfo> ParseItems(RepeatedField<Item> items,
        IReadOnlyDictionary<string, WaveeTrackOrEpisodeOrArtist>? tracks)
    {
        Span<PlaylistTrackInfo> output = new PlaylistTrackInfo[items.Count];
        tracks ??= new Dictionary<string, WaveeTrackOrEpisodeOrArtist>();
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            output[index] = new PlaylistTrackInfo(item.Uri)
            {
                AddedBy = item.Attributes.HasAddedBy
                    ? item.Attributes.AddedBy
                    : null,
                AddedAt = item.Attributes.HasTimestamp
                    ? DateTimeOffset.FromUnixTimeMilliseconds(item.Attributes.Timestamp)
                    : null,
                UniqueItemId = item.Attributes.HasItemId
                    ? item.Attributes.ItemId.ToBase64()
                    : null,
                Item = tracks.TryGetValue(item.Uri,
                    out var track)
                    ? track
                    : null,
                LastPlayedAt = null
            };
        }

        return ImmutableArray.Create(output);
    }
}