using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using Wavee.Metadata.Common;

namespace Wavee.UI.WinUI;

public class GetFirstImageSafeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is CoverImage[] images)
        {
            var url = images?.OrderBy(x=> x.Height.IfNone(0))?.FirstOrDefault().Url;
            if (string.IsNullOrEmpty(url))
            {
                return DependencyProperty.UnsetValue;
            }

            var bmp = new BitmapImage
            {
                UriSource = new Uri(url)
            };
            return bmp;
        }

        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}