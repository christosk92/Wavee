using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Eum.UWP.XamlConverters
{
    internal sealed class IsNullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var negate = parameter is "n" ? true : false;
            if (value is null)
            {
                return negate ? Visibility.Visible : Visibility.Collapsed;
            }
            return negate ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class IsNullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var ifNull = bool.Parse(parameter.ToString());
            if (value is null)
            {
                return ifNull;
            }
            return !ifNull;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            var ifNull = bool.Parse(parameter.ToString());
            return ((bool) value) ? ifNull : !ifNull;
        }
    }
}
