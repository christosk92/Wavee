using System;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.UI.Xaml.Data;

namespace Eum.UI.WinUI.XamlConverters
{
    public class TimeSpanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is TimeSpan ts)
            {
                return ts.Humanize(2, countEmptyUnits: true, minUnit: TimeUnit.Second);
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
