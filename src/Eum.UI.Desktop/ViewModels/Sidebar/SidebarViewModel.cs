using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Eum.Enums;
using Eum.UI.ViewModels.ForYou;
using Eum.UI.ViewModels.Library;
using Eum.UI.ViewModels.Playlists;
using Eum.UI.ViewModels.Users;

namespace Eum.UI.ViewModels.Sidebar;
[INotifyPropertyChanged]
public sealed partial class SidebarViewModel
{
    [ObservableProperty]
    private SidebarItemViewModel? _selectedSidebarItem;

    private UserViewModelBase _user;

    public SidebarViewModel(UserViewModelBase user)
    {
        _user = user;

        foreach (var playlistViewModel in user.Playlists)
        {
            SidebarItems.Add(playlistViewModel);
        }
        _user.PlaylistAdded += UserOnPlaylistAdded;
    }

    public void Deconstruct()
    {
        _user.PlaylistAdded += UserOnPlaylistAdded;
    }

    private void UserOnPlaylistAdded(object? sender, PlaylistViewModel e)
    {
        SidebarItems.Add(e);
    }

    public ObservableCollection<ISidebarItem>
        SidebarItems
    { get; } = new()
    {
        new SidebarItemHeader
        {
            Title = "For you"
        },
        new HomeViewModel(),
        new ForYouViewModel(),
        new SidebarItemHeader
        {
            Title = "Library"
        },
        new LibraryViewModel(EntityType.Album),
        new LibraryViewModel(EntityType.Artist),
        new LibraryViewModel(EntityType.Track),
        new LibraryViewModel(EntityType.Show),
        new SidebarPlaylistHeader()
    };


    private void UnRegisterPlaylistAdded(UserViewModelBase userViewModelBase)
    {
        userViewModelBase.PlaylistAdded -= VOnPlaylistAdded;
    }

    private void RegisterPlaylistAdded(UserViewModelBase userViewModelBase)
    {
        userViewModelBase.PlaylistAdded += VOnPlaylistAdded;
    }

    private void VOnPlaylistAdded(object sender, PlaylistViewModel e)
    {
        SidebarItems.Add(e);
    }
}