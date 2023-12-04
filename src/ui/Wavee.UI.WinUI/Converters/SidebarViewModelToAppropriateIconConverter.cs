using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using Microsoft.UI.Xaml.Media;
using Wavee.UI.Features.Library.ViewModels;
using Wavee.UI.Features.Listen;
using Wavee.UI.Features.NowPlaying.ViewModels;
using Wavee.UI.Features.Library.ViewModels.Artist;
using Wavee.UI.Features.Library.ViewModels.Album;

namespace Wavee.UI.WinUI.Converters;
internal sealed class SidebarViewModelToAppropriateIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var icon = value switch
        {
            ListenViewModel => Create('\uE794', MediaPlayerIcons),
            LibrariesViewModel => Create('\uE8F1', SegoeFluentIcons),
            NowPlayingViewModel => Create('\uE93D', MediaPlayerIcons),
            LibrarySongsViewModel => Create('\uE940', MediaPlayerIcons),
            LibraryAlbumsViewModel => Create('\uE93C', MediaPlayerIcons),
            LibraryArtistsViewModel => Create('\uEBDA', SegoeFluentIcons),
            LibraryPodcastsViewModel => Create('\uEB44', SegoeFluentIcons),
            _ => null
        };

        return icon;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

    private static IconElement Create(char glyph, FontFamily fontFamily)
    {
        return new FontIcon
        {
            FontFamily = fontFamily, Glyph = glyph.ToString()
        };
    }
    private static FontFamily SegoeFluentIcons => (FontFamily)Microsoft.UI.Xaml.Application.Current.Resources["FluentIcons"]!;
    private static FontFamily MediaPlayerIcons => (FontFamily)Microsoft.UI.Xaml.Application.Current.Resources["MediaPlayerIcons"]!;
}