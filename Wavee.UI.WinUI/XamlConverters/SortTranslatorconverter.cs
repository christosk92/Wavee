using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Data;
using Wavee.UI.Interfaces.Services;
using Wavee.UI.ViewModels.Libray;

namespace Wavee.UI.WinUI.XamlConverters
{
    public sealed class SortTranslatorconverter : IValueConverter
    {
        private static readonly IStringLocalizer _localizer;
        static SortTranslatorconverter()
        {
            _localizer = Ioc.Default.GetRequiredService<IStringLocalizer>();
        }
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not SortOption s) return null;

            //the key should be /Sorts/{s}
            return _localizer.GetValue($"/Sorts/{s.ToString()}");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
