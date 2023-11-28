using CommunityToolkit.Mvvm.ComponentModel;
using Wavee.UI.Features.Library.ViewModels.Artist;
using Wavee.UI.Features.Navigation.ViewModels;

namespace Wavee.UI.Features.Library.ViewModels;

public sealed class LibrariesViewModel : NavigationItemViewModel
{
    public LibrariesViewModel(
        LibrarySongsViewModel songs,
        LibraryArtistsViewModel artists,
        LibraryAlbumsViewModel albums,
        LibraryPodcastsViewModel podcasts)
    {
        Songs = songs;
        Artists = artists;
        Albums = albums;
        Podcasts = podcasts;
        Children = new NavigationItemViewModel[]
        {
            songs,
            artists,
            albums,
            podcasts
        };

        SelectedItem = songs;
    }

    public override NavigationItemViewModel[] Children { get; }
    public NavigationItemViewModel Songs { get; }
    public NavigationItemViewModel Albums { get; }
    public NavigationItemViewModel Artists { get; }
    public NavigationItemViewModel Podcasts { get; }
}