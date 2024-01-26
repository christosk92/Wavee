using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using Microsoft.UI.Xaml.Media;
using Wavee.UI.ViewModels.Feed;
using Wavee.UI.ViewModels.Library;
using Wavee.UI.ViewModels.NowPlaying;

namespace Wavee.UI.WinUI.Converters;
internal sealed class ViewModelToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        double? fontSize = null;
        var valueAsStr = parameter?.ToString();
        if (!string.IsNullOrEmpty(valueAsStr) && double.TryParse(valueAsStr, out var x))
        {
            fontSize = x;
        }

        var icon = value switch
        {
            FeedViewModel => Create('\uE794', MediaPlayerIcons),
            LibraryRootViewModel => Create('\uE8F1', SegoeFluentIcons),
            NowPlayingViewModel => Create('\uE93D', MediaPlayerIcons),
            LibraryTracksViewModel => Create('\uE940', MediaPlayerIcons),
            LibraryAlbumsViewModel => Create('\uE93C', MediaPlayerIcons),
            LibraryArtistsViewModel => Create('\uEBDA', SegoeFluentIcons),
            _ => null
        };

        return icon;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

    private static IconElement Create(char glyph, FontFamily fontFamily, double? fontSize = null)
    {
        return new FontIcon
        {
            FontFamily = fontFamily,
            Glyph = glyph.ToString(),
            FontSize = fontSize ?? 16
        };
    }
    private static FontFamily SegoeFluentIcons => (FontFamily)Microsoft.UI.Xaml.Application.Current.Resources["FluentIcons"]!;
    private static FontFamily MediaPlayerIcons => (FontFamily)Microsoft.UI.Xaml.Application.Current.Resources["MediaPlayerIcons"]!;
}