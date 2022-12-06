using System;
using Microsoft.UI.Xaml.Data;

namespace Eum.UI.WinUI.XamlConverters
{
    public class PlaycountToStringConverter
        : IValueConverter
    {
        public Type EnumType { get; set; }


        public object Convert(
            object value,
            Type targetType,
            object parameter,
            string language)
        {
            return value switch
            {
                int count => count > 0 ? $"{count:#,##0}" : "< 1,000",
                long val => val > 0 ? $"{val:#,##0}" : "< 1,000",
                _ => $"< 1,00"
            };
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            string language)
        {
            throw new NotImplementedException();
        }
    }
}
