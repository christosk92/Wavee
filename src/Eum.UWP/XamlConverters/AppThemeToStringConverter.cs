using System;
using Windows.UI.Xaml.Data;
using Eum.UI.ViewModels.Settings;

namespace Eum.UWP.XamlConverters
{
    class AppThemeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is AppTheme appTheme)
            {
                return appTheme switch
                {
                    AppTheme.Dark => "Dark",
                    AppTheme.Light => "Light",
                    AppTheme.SystemDefault => "System Default",
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            return "System Default";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
