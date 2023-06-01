using Wavee.Core.Ids;

namespace Wavee.UI.ViewModels.Playlists;

public readonly record struct PlaylistShortItem
{
    public required AudioId Id { get; init; }
    public required string Name { get; init; }
}