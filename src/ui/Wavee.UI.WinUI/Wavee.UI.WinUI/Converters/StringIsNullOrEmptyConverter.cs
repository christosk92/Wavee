using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;
using System;

namespace Wavee.UI.WinUI.Converters;

public class StringIsNullOrEmptyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isNullOrEmpty = string.IsNullOrEmpty(value as string);

        // Determine the visibility when the string is NOT null or empty

        var visibilityWhenNotNullOrEmpty = parameter?.ToString()?.ToLower() switch
        {
            "collapsed" => isNullOrEmpty ? Visibility.Collapsed : Visibility.Visible,
            "visible" => isNullOrEmpty ? Visibility.Visible : Visibility.Collapsed,
            _ => isNullOrEmpty ? Visibility.Collapsed : Visibility.Visible
        };

        return visibilityWhenNotNullOrEmpty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}