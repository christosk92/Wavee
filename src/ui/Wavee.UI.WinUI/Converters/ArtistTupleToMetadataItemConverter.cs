using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml.Data;

namespace Wavee.UI.WinUI.Converters;

public sealed class ArtistTupleToMetadataItemConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is IReadOnlyCollection<(string, string)> x)
        {
            return x.Select(f => new MetadataItem
            {
                Label = f.Item2
            });
        }

        return System.Array.Empty<MetadataItem>();
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}