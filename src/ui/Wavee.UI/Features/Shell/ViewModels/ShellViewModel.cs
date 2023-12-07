using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using Mediator;
using Wavee.UI.Features.Artist.ViewModels;
using Wavee.UI.Features.Library.ViewModels;
using Wavee.UI.Features.Library.ViewModels.Album;
using Wavee.UI.Features.Library.ViewModels.Artist;
using Wavee.UI.Features.Listen;
using Wavee.UI.Features.Navigation;
using Wavee.UI.Features.Navigation.ViewModels;
using Wavee.UI.Features.NowPlaying.ViewModels;
using Wavee.UI.Features.Playback.ViewModels;
using Wavee.UI.Features.Search.ViewModels;
using Wavee.UI.Test;

namespace Wavee.UI.Features.Shell.ViewModels;

public sealed class ShellViewModel : ObservableObject
{
    private Func<IUIDispatcher> _dispatcherFactory;
    private NavigationItemViewModel? _selectedItem;

    public ShellViewModel(
        ListenViewModel listen,
        LibrariesViewModel library,
        NowPlayingViewModel nowPlaying,
        INavigationService navigation,
        PlaybackViewModel playback,
        SearchViewModel search,
        IMediator mediator,
        IServiceProvider serviceProvider)
    {
        TopNavItems = new object[]
        {
            listen,
            library,
            nowPlaying
        };
        SelectedItem = listen;
        Navigation = navigation;
        Playback = playback;
        Search = search;
        Mediator = mediator;
        _dispatcherFactory = () =>
        {
            return (IUIDispatcher)serviceProvider.GetService(typeof(IUIDispatcher));
        };

        navigation.NavigatedTo += (sender, o) =>
        {
            var type = o.GetType();
            if (type == typeof(ListenViewModel))
            {
                SelectedItem = listen;
            }
            else if (type == typeof(LibrariesViewModel))
            {
                SelectedItem = library;
            }
            else if (type == typeof(NowPlayingViewModel))
            {
                SelectedItem = nowPlaying;
            }
            else if (type == typeof(LibrarySongsViewModel))
            {
                SelectedItem = library;
                library.SelectedItem = library.Songs;
            }
            else if (type == typeof(LibraryAlbumsViewModel))
            {
                SelectedItem = library;
                library.SelectedItem = library.Albums;
            }
            else if (type == typeof(LibraryArtistsViewModel))
            {
                SelectedItem = library;
                library.SelectedItem = library.Artists;
            }
            else if (type == typeof(LibraryPodcastsViewModel))
            {
                SelectedItem = library;
                library.SelectedItem = library.Podcasts;
            }
            else if (type == typeof(ArtistViewModel))
            {
                SelectedItem = o as ArtistViewModel;
                SelectedItem.SelectedItem = SelectedItem.Children[0];
            }
            else
            {
                SelectedItem = new NothingSelectedViewModel();
            }
        };
    }

    public INavigationService Navigation { get; }
    public IReadOnlyCollection<object> TopNavItems { get; }

    public NavigationItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    public PlaybackViewModel Playback { get; }
    public SearchViewModel Search { get; }
    public IMediator Mediator { get; }
    public IUIDispatcher Dispatcher => _dispatcherFactory();
}

public sealed class NothingSelectedViewModel : NavigationItemViewModel
{
}