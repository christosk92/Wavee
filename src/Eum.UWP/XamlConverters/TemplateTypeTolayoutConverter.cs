using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Eum.UI.ViewModels.Artists;

namespace Eum.UWP.XamlConverters;

public class TemplateTypeTolayoutConverter : DependencyObject, IValueConverter
{
    public static readonly DependencyProperty HorizontalStackProperty = DependencyProperty.Register(nameof(HorizontalStack), typeof(object), typeof(TemplateTypeTolayoutConverter), new PropertyMetadata(default(object)));
    public static readonly DependencyProperty GridLayoutProperty = DependencyProperty.Register(nameof(GridLayout), typeof(object), typeof(TemplateTypeTolayoutConverter), new PropertyMetadata(default(object)));
    public static readonly DependencyProperty VerticalStackProperty = DependencyProperty.Register(nameof(VerticalStack), typeof(object), typeof(TemplateTypeTolayoutConverter), new PropertyMetadata(default(object)));

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TemplateTypeOrientation templateType)
        {
            return templateType switch
            {
                TemplateTypeOrientation.VerticalStack => VerticalStack,
                TemplateTypeOrientation.HorizontalStack => HorizontalStack,
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

    public object VerticalStack
    {
        get => (object) GetValue(VerticalStackProperty);
        set => SetValue(VerticalStackProperty, value);
    }
}


public class ItemTemplateTemplateSelector : DataTemplateSelector
{
    // protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    // {
    //     if (item is DiscographyViewModel v)
    //     {
    //         return v.TemplateType switch
    //         {
    //             TemplateTypeOrientation.VerticalStack => VerticalStack,
    //             TemplateTypeOrientation.HorizontalStack => HorizontalStack,
    //             TemplateTypeOrientation.Grid => GridLayout
    //         };
    //     }
    //     return base.SelectTemplateCore(item, container);
    // }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        if (item is DiscographyViewModel v)
        {
            return v.TemplateType switch
            {
                TemplateTypeOrientation.VerticalStack => VerticalStack,
                TemplateTypeOrientation.HorizontalStack => HorizontalStack,
                TemplateTypeOrientation.Grid => GridLayout
            };
        }
        return base.SelectTemplateCore(item);
    }

    public DataTemplate GridLayout { get; set; }

    public DataTemplate HorizontalStack { get; set; }

    public DataTemplate VerticalStack { get; set; }
}
