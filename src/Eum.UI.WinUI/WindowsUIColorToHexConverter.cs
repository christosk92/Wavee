using System;
using Windows.UI;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml.Data;

namespace Eum.UI.WinUI;

public class WindowsUIColorToHexConverter   : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string c)
        {
            return c.ToColor();
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Color c)
        {
            return c.ToHex();
        }

        return null;
    }
}