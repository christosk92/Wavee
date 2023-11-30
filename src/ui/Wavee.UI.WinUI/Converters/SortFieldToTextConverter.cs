using System;
using Microsoft.UI.Xaml.Data;
using Wavee.UI.Domain.Library;
using Wavee.UI.Entities.Artist;

namespace Wavee.UI.WinUI.Converters;

sealed class SortFieldToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value switch
        {
            nameof(LibraryItem<SimpleArtistEntity>.AddedAt) => "Added At",
            nameof(LibraryItem<SimpleArtistEntity>.Item.Name) => "Alphabetical",
            "Recents" => "Recently played"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}