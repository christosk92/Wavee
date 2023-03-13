using System;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.UI.Xaml.Data;

namespace Wavee.UI.WinUI.XamlConverters
{
    public class TimeSpanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var ts = value switch
            {
                TimeSpan t => t,
                double val => TimeSpan.FromMilliseconds(val),
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };
            return ts.Humanize(2, countEmptyUnits: true, minUnit: TimeUnit.Second);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
