using System;
using Microsoft.UI.Xaml.Data;
using Wavee.UI.Features.Artist.ViewModels;
using Wavee.UI.Features.Library.ViewModels;
using Wavee.UI.Features.Library.ViewModels.Album;
using Wavee.UI.Features.Library.ViewModels.Artist;
using Wavee.UI.Features.Listen;
using Wavee.UI.Features.NowPlaying.ViewModels;
using Wavee.UI.Features.Playlists.ViewModel;
using Wavee.UI.Features.RightSidebar.ViewModels;
using Wavee.UI.Features.Shell.ViewModels;

namespace Wavee.UI.WinUI.Converters;

internal sealed class SidebarViewModelToAppropriateTitleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value switch
        {
            ListenViewModel => "Listen",
            LibrariesViewModel => "Library",
            NowPlayingViewModel => "Now Playing",
            LibrarySongsViewModel => "Songs",
            LibraryAlbumsViewModel => "Albums",
            LibraryArtistsViewModel => "Artists",
            LibraryPodcastsViewModel => "Podcasts",
            ArtistOverviewViewModel => "Discography",
            ArtistAboutViewModel => "About",
            ArtistRelatedContentViewModel => "Related Content",

            RightSidebarLyricsViewModel => "Lyrics",
            RightSidebarVideoViewModel => "Video",
            RightSidebarQueueViewModel => "Queue",

            PlaylistsViewModel => "Playlists",
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}