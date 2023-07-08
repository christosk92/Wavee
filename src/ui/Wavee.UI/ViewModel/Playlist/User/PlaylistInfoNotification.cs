namespace Wavee.UI.ViewModel.Playlist.User;

public sealed class PlaylistInfoNotification
{
    public required PlaylistInfo[] Playlists { get; init; }
    public required PlaylistInfoChangeType ChangeType { get; init; }
}