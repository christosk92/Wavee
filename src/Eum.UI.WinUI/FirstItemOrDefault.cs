using System;
using System.Collections;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Eum.UI.WinUI;

public class FirstItemOrDefault : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is IEnumerable t)
        {
            return t.Cast<object>().FirstOrDefault();
        }

        return default;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}