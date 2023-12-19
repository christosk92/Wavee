// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Wavee.UI.WinUI.Converters
{
    public class BoolToVisibilityConverter : DependencyObject, IValueConverter
    {
        public static readonly DependencyProperty FalseValueProperty = DependencyProperty.Register(nameof(FalseValue), typeof(Visibility), typeof(BoolToVisibilityConverter), new PropertyMetadata(default(Visibility)));
        public static readonly DependencyProperty TrueValueProperty = DependencyProperty.Register(nameof(TrueValue), typeof(Visibility), typeof(BoolToVisibilityConverter), new PropertyMetadata(default(Visibility)));

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b)
            {
                return b ? TrueValue : FalseValue;
            }

            return FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        public Visibility FalseValue
        {
            get => (Visibility)GetValue(FalseValueProperty);
            set => SetValue(FalseValueProperty, value);
        }

        public Visibility TrueValue
        {
            get => (Visibility)GetValue(TrueValueProperty);
            set => SetValue(TrueValueProperty, value);
        }
    }
}
