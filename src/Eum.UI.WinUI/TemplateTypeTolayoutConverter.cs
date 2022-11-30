using System;
using Eum.UI.WinUI.Views.Shell;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace Eum.UI.WinUI;

public class TemplateTypeTolayoutConverter : DependencyObject, IValueConverter
{
    public static readonly DependencyProperty HorizontalStackProperty = DependencyProperty.Register(nameof(HorizontalStack), typeof(object), typeof(TemplateTypeTolayoutConverter), new PropertyMetadata(default(object)));
    public static readonly DependencyProperty GridLayoutProperty = DependencyProperty.Register(nameof(GridLayout), typeof(object), typeof(TemplateTypeTolayoutConverter), new PropertyMetadata(default(object)));

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TemplateTypeOrientation templateType)
        {
            return templateType switch
            {
                TemplateTypeOrientation.VerticalStack => HorizontalStack,
                TemplateTypeOrientation.Grid => GridLayout
            };
        }

        return GridLayout;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

    public object HorizontalStack
    {
        get => (object) GetValue(HorizontalStackProperty);
        set => SetValue(HorizontalStackProperty, value);
    }

    public object GridLayout
    {
        get => (object) GetValue(GridLayoutProperty);
        set => SetValue(GridLayoutProperty, value);
    }
}