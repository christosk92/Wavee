using Mediator;
using Wavee.Spotify.Common;
using Wavee.UI.Domain.Playlist;

namespace Wavee.UI.Features.Playlists.Queries;

public sealed class GetPlaylistTracksIdsQuery : IQuery<IReadOnlyCollection<PlaylistTrackInfo>>
{
    public required string PlaylistId { get; init; }
}