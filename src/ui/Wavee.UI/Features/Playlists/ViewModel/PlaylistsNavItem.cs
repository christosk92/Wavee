using Wavee.UI.Features.Navigation.ViewModels;

namespace Wavee.UI.Features.Playlists.ViewModel;

public sealed class PlaylistsNavItem : NavigationItemViewModel
{
    private PlaylistSidebarItemViewModel? _selectedPlaylist;
    private bool? _showSidebar = false;
    private double _sidebarWidth = 200;

    public PlaylistsNavItem(PlaylistsViewModel playlists)
    {
        Playlists = playlists;
    }

    public PlaylistsViewModel Playlists { get; }

    public PlaylistSidebarItemViewModel? SelectedPlaylist
    {
        get => _selectedPlaylist;
        set => SetProperty(ref _selectedPlaylist, value);
    }

    public bool? ShowSidebar
    {
        get => _showSidebar;
        set => SetProperty(ref _showSidebar, value);
    }

    public double SidebarWidth
    {
        get => _sidebarWidth;
        set => SetProperty(ref _sidebarWidth, value);
    }
}