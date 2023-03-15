using System;
using Microsoft.UI.Xaml.Data;

namespace Wavee.UI.WinUI.XamlConverters;
public class OrangeTextHighlighterConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null)
            return null;

        string text = value.ToString();
        if (!text.Contains(":"))
            return text;

        int index = text.IndexOf(":");
        return text.Substring(index + 1);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}