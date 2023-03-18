using System;
using Microsoft.UI.Xaml.Data;

namespace Wavee.UI.WinUI.XamlConverters
{
    public class HalfValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return ((double)value) / 2;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
