namespace Wavee.UI.ViewModel.Playlist.User;

public sealed class PlaylistInfo
{
    public required string Id { get; init; }
    public required string OwnerId { get; init; }
    public required string Name { get; init; }
    public required bool IsFolder { get; init; }
    public required List<PlaylistInfo> Children { get; init; }
    public required int FixedIndex { get; set; }
}