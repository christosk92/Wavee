using System;
using Microsoft.UI.Xaml.Data;
using Wavee.UI.Domain.Artist;
using Wavee.UI.Domain.Library;

namespace Wavee.UI.WinUI.Converters;

sealed class SortFieldToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value switch
        {
            ArtistLibrarySortField.RecentlyAdded => "Added At",
            ArtistLibrarySortField.Alphabetical => "Alphabetical",
            ArtistLibrarySortField.Recents => "Recently played"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}