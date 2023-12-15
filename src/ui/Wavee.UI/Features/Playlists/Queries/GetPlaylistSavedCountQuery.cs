using Mediator;

namespace Wavee.UI.Features.Playlists.Queries;

public sealed class GetPlaylistSavedCountQuery : IQuery<ulong>
{
    public required string PlaylistId { get; init; }
}