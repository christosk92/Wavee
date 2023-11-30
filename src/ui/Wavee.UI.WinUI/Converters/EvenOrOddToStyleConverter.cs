using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Wavee.UI.WinUI.Converters;

sealed class EvenOrOddToStyleConverter : DependencyObject, IValueConverter
{
    public static readonly DependencyProperty EvenStyleProperty = DependencyProperty.Register(nameof(EvenStyle), typeof(Style), typeof(EvenOrOddToStyleConverter), new PropertyMetadata(default(Style)));
    public static readonly DependencyProperty OddStyleProperty = DependencyProperty.Register(nameof(OddStyle), typeof(Style), typeof(EvenOrOddToStyleConverter), new PropertyMetadata(default(Style)));

    public Style EvenStyle
    {
        get => (Style)GetValue(EvenStyleProperty);
        set => SetValue(EvenStyleProperty, value);
    }

    public Style OddStyle
    {
        get => (Style)GetValue(OddStyleProperty);
        set => SetValue(OddStyleProperty, value);
    }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int i)
        {
            return i % 2 == 0 ? EvenStyle : OddStyle;
        }

        return EvenStyle;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}