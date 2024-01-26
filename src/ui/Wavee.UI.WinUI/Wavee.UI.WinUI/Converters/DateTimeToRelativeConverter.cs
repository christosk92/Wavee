using System;
using System.Globalization;
using Microsoft.UI.Xaml.Data;
using DateTimeOffset = System.DateTimeOffset;
using TimeSpan = System.TimeSpan;
using Type = System.Type;

namespace Wavee.UI.WinUI.Converters;

public sealed class DateTimeToRelativeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DateTimeOffset x)
        {
            var now = DateTimeOffset.Now;
            var timespan = now - x;

            // If the timespan is less than 1 minute, return "Just now".
            if (timespan < TimeSpan.FromMinutes(1))
            {
                return "Just now";
            }
            // If the timespan is less than 1 hour, return "x minutes ago".
            else if (timespan < TimeSpan.FromHours(1))
            {
                return $"{timespan.Minutes} minutes ago";
            }  // If the timespan is less than 1 day, return "x hours ago".
            else if (timespan < TimeSpan.FromDays(1))
            {
                return $"{timespan.Hours} hours ago";
            }
            // If the date is within the current year, return "Month Day" format.
            else if (x.Year == now.Year)
            {
                return x.ToString("MMM d", CultureInfo.InvariantCulture);
            }
            // Otherwise, return "Month Day, Year" format.
            else
            {
                return x.ToString("MMM d, yyyy", CultureInfo.InvariantCulture);
            }
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}