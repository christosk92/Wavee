using System;
using Microsoft.UI.Xaml.Data;
using Wavee.UI.Features.Library.ViewModels;
using Wavee.UI.Features.Library.ViewModels.Artist;
using Wavee.UI.Features.Listen;
using Wavee.UI.Features.NowPlaying.ViewModels;

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
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}