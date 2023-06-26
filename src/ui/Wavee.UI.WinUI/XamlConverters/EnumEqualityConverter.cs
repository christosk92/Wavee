using System;
using Microsoft.UI.Xaml.Data;

namespace Wavee.UI.WinUI.XamlConverters;

internal sealed class EnumEqualityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var enumValue = (Enum)value;
        //parse the parameter to an enum
        var parameterValue = Enum.Parse(enumValue.GetType(), parameter.ToString());
        return enumValue.Equals(parameterValue);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}