using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wavee.UI.Providers;
using Wavee.UI.Services;
using Wavee.UI.ViewModels.Feed;
using Wavee.UI.ViewModels.Library;
using Wavee.UI.ViewModels.NowPlaying;
using Wavee.UI.ViewModels.Playlists;

namespace Wavee.UI.ViewModels.Shell;

public sealed class ShellViewModel : ObservableObject
{
    private readonly ObservableCollection<IWaveeUIAuthenticatedProfile> _profiles = new();
    public ShellViewModel(
        FeedViewModel feed,
        LibraryRootViewModel library,
        NowPlayingViewModel nowPlaying,
        PlaylistsViewModel playlists,
        RightSidebarViewModel rightSidebar,
        IWaveeUIAuthenticatedProfile profile)
    {
        Playlists = playlists;

        RootNavigationClickedCommand = new RelayCommand<object>(RootNavigationClicked);

        NowPlaying = nowPlaying;
        RootNavigationItems = [feed, library];
        RightSidebar = rightSidebar;
        PrepareProfile(profile);
    }

    public void SetNavigationController(INavigationController navigationController)
    {
        NavigationController = navigationController;
    }
    public PlaylistsViewModel Playlists { get; }
    public ICommand RootNavigationClickedCommand { get; }
    public IReadOnlyCollection<IHasProfileViewModel> RootNavigationItems { get; }
    public INavigationController NavigationController { get; private set; }
    public NowPlayingViewModel NowPlaying { get; }
    public RightSidebarViewModel RightSidebar { get; }

    public void PrepareProfile(IWaveeUIAuthenticatedProfile profile)
    {
        _profiles.Add(profile);

        Playlists.AddFromProfile(profile);

        foreach (var rootNav in RootNavigationItems)
        {
            rootNav.AddFromProfile(profile);
        }
        NowPlaying.AddFromProfile(profile);
    }

    public void RemoveProfile(IWaveeUIAuthenticatedProfile profile)
    {
        _profiles.Remove(profile);

        Playlists.RemoveFromProfile(profile);

        foreach (var rootNav in RootNavigationItems)
        {
            rootNav.RemoveFromProfile(profile);
        }
    }


    private void RootNavigationClicked(object? obj)
    {
        NavigationController.NavigateTo(obj);
    }
}