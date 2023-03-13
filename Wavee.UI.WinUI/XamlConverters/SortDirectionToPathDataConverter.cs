using System;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml.Data;

namespace Wavee.UI.WinUI.XamlConverters
{
    public class SortDirectionToPathDataConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not SortDirection sortDirection) return "M 0 0 L 0 0 L 0 0 Z";
            return sortDirection switch
            {
                SortDirection.Ascending => "M 0 0 L 10 10 L 20 0 Z",
                SortDirection.Descending => "M 0 0 L 10 -10 L 20 0 Z",
                _ => "M 0 0 L 0 0 L 0 0 Z"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}