namespace Wavee.UI.ViewModel.Playlist.Headers;

public interface IPlaylistHeader
{
     string Owner { get; }
     string Name { get; }
     string Description { get; }
     string? MadeForUsername { get; }
     bool ShouldShowMadeFor { get; }
     bool IsCollab { get; }
}