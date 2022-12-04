using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Eum.UI.WinUI.XamlConverters
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
            return Visibility.Collapsed;
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
