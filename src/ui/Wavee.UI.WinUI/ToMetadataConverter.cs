using System;
using System.Linq;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Wavee.Metadata.Artist;

namespace Wavee.UI.WinUI;

public class ToMetadataConverter : IValueConverter  
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TrackArtist[] artists)
        {
            return artists.Select(x => new MetadataItem
            {
                Label = x.Name
            });
        }

        return Enumerable.Empty<MetadataItem>();
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}