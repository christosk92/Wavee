using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Wavee.UI.WinUI.XamlConverters;

public class GetRowStyleConverter : DependencyObject, IValueConverter
{
    public static readonly DependencyProperty EvenProperty = DependencyProperty.Register(nameof(Even), typeof(Style), typeof(GetRowStyleConverter), new PropertyMetadata(default(Style)));
    public static readonly DependencyProperty OddProperty = DependencyProperty.Register(nameof(Odd), typeof(Style), typeof(GetRowStyleConverter), new PropertyMetadata(default(Style)));

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value switch
        {
            int index => index % 2 == 0 ? Even : Odd,
            _ => Even
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

    public Style Even
    {
        get => (Style) GetValue(EvenProperty);
        set => SetValue(EvenProperty, value);
    }

    public Style Odd
    {
        get => (Style) GetValue(OddProperty);
        set => SetValue(OddProperty, value);
    }
}