using Wavee.UI.Features.Navigation.ViewModels;

namespace Wavee.UI.Features.Playlists.ViewModel;

public sealed class PlaylistsNavItem : NavigationItemViewModel
{
    private bool? _showSidebar = false;
    public PlaylistsNavItem(PlaylistsViewModel playlists)
    {
        
    }

    public PlaylistsViewModel Playlists { get; }

    public bool? ShowSidebar
    {
        get => _showSidebar;
        set => SetProperty(ref _showSidebar, value);
    }
}