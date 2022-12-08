using System;
using System.Linq;
using Windows.UI.Xaml.Data;
using Eum.UI;
using Eum.UI.ViewModels.Playback;
using Microsoft.Toolkit.Uwp.UI.Controls;

namespace Eum.UWP;

public class IdWithTitleToMetadataConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is IdWithTitle[] idWith)
        {
            return idWith.Select(a => new MetadataItem
            {
                Label = a.Title,
                Command = Commands.To(a.Id.Type),
                CommandParameter = a.Id,
            });
        }

        return Array.Empty<MetadataItem[]>();
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
