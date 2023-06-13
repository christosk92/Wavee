using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Wavee.UI.WinUI;

public class ToImageSourceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string s)
        {
            var bitmapImage = new BitmapImage
            {
                // bitmapImage.DecodePixelHeight = 200;
                // bitmapImage.DecodePixelWidth = 200;
                UriSource = new System.Uri(s, UriKind.RelativeOrAbsolute)
            };
            return bitmapImage;
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}