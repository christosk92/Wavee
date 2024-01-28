using System;
using Microsoft.UI.Xaml.Data;

namespace Wavee.UI.WinUI.Converters;

public class MilliSecondsToTimestampConverter : IValueConverter
{

    public static string ConvertTo(TimeSpan? dur)
    {
        if (dur is null) return "--:--";
        return $"{dur.Value.Minutes:D2}:{dur.Value.Seconds:D2}";
    }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double x)
        {
            return ConvertTo(TimeSpan.FromMilliseconds(x));
        }

        return "--:--";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}