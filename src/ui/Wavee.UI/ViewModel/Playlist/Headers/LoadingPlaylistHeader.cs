namespace Wavee.UI.ViewModel.Playlist.Headers;

public sealed class LoadingPlaylistHeader : IPlaylistHeader
{
    public string Owner { get; }
    public string Name { get; }
    public string Description { get; }
    public string? MadeForUsername { get; }
    public bool ShouldShowMadeFor { get; }
    public bool IsCollab { get; }
}