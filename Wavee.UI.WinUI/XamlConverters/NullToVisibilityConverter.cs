using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Wavee.UI.WinUI.XamlConverters
{
    internal class NullToVisibilityConverter : DependencyObject, IValueConverter
    {
        public static readonly DependencyProperty NullProperty = DependencyProperty.Register(nameof(Null), typeof(Visibility), typeof(NullToVisibilityConverter), new PropertyMetadata(default(Visibility)));
        public static readonly DependencyProperty NonNullProperty = DependencyProperty.Register(nameof(NonNull), typeof(Visibility), typeof(NullToVisibilityConverter), new PropertyMetadata(default(Visibility)));

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is null ? Null : NonNull;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        public Visibility Null
        {
            get => (Visibility)GetValue(NullProperty);
            set => SetValue(NullProperty, value);
        }

        public Visibility NonNull
        {
            get => (Visibility)GetValue(NonNullProperty);
            set => SetValue(NonNullProperty, value);
        }
    }
}
