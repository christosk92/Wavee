using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Wavee.UI.Domain.Artist;
using Wavee.UI.Domain.Library;

namespace Wavee.UI.WinUI.Converters;

sealed class SortFieldToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value switch
        {
            ArtistLibrarySortField.RecentlyAdded => Create('\uED0E', SegoeFluentIcons),
            ArtistLibrarySortField.Alphabetical => Create('\uE97E', SegoeFluentIcons),
            ArtistLibrarySortField.Recents => Create('\uE823', SegoeFluentIcons),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

    private static IconElement Create(char glyph, FontFamily fontFamily)
    {
        return new FontIcon
        {
            FontFamily = fontFamily,
            Glyph = glyph.ToString()
        };
    }
    private static FontFamily SegoeFluentIcons => (FontFamily)Microsoft.UI.Xaml.Application.Current.Resources["FluentIcons"]!;
    private static FontFamily MediaPlayerIcons => (FontFamily)Microsoft.UI.Xaml.Application.Current.Resources["MediaPlayerIcons"]!;
}