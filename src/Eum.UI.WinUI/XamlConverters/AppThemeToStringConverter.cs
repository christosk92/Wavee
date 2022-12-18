using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eum.UI.ViewModels.Settings;
using Microsoft.UI.Xaml.Data;

namespace Eum.UI.WinUI.XamlConverters
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
