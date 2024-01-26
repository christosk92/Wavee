using System;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using Wavee.Spfy.Items;

namespace Wavee.UI.WinUI.Converters;

public sealed class WaveeItemToImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var url = value switch
        {
            ISpotifyItem sp => sp.Images.HeadOrNone().Map(x => x.Url),
            _ => Option<string>.None
        };
        if (url.IsSome)
        {
            if (!int.TryParse(parameter?.ToString(), out var size))
            {
                size = 300;
            }
            var bmp = new BitmapImage
            {
                UriSource = new Uri(url.ValueUnsafe()),
                DecodePixelHeight = size,
                DecodePixelWidth = size
            };
            return bmp;
        }

        return null;
        //
        // return url.Map(x =>
        // {
        //     var bmp = new BitmapImage
        //     {
        //         UriSource = new Uri(x),
        //         DecodePixelHeight = 80,
        //         DecodePixelWidth = 80
        //     };
        //     return bmp;
        // }).ValueUnsafe();
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}