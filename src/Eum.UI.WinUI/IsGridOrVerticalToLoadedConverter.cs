using System;
using Eum.UI.ViewModels.Artists;
using Microsoft.UI.Xaml.Data;

namespace Eum.UI.WinUI;

public class IsGridOrVerticalToLoadedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TemplateTypeOrientation t)
        {
            if (Enum.TryParse(typeof(TemplateTypeOrientation), parameter?.ToString(), out var et))
            {
                TemplateTypeOrientation param = (TemplateTypeOrientation) et;
                return param == t;
            }
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
