using System;
using Microsoft.UI.Xaml.Data;

namespace Eum.UI.WinUI.XamlConverters
{
    public class MsToTimeStampConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var timeStamp = value as double? ?? value as int?;
            if(timeStamp == null)
            {
                return "00:00";
            }
            TimeSpan timeSpan = TimeSpan.FromMilliseconds((double)timeStamp);
            return string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class IntToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var timeStamp = value as int?;
            if (timeStamp == null)
            {
                return (double)0.0;
            }
            return (double)timeStamp;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
