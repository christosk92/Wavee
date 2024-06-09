using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Wavee.UI.WinUI.Converters;

public static class Color
{
    public static SolidColorBrush WithAlphaPercentage(string colorCode, int percentage)
    {
        if (string.IsNullOrEmpty(colorCode))
        {
            return (SolidColorBrush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"];
        }
        var color = FromHex(colorCode);
        color.A = (byte)(255 * percentage / 100);
        return new SolidColorBrush(color);
    }

    private static Windows.UI.Color FromHex(string colorCode)
    {


        colorCode = colorCode.Replace("#", string.Empty);
        byte a = 255;
        byte r = 0;
        byte g = 0;
        byte b = 0;

        if (colorCode.Length == 8)
        {
            a = Convert.ToByte(colorCode.Substring(0, 2), 16);
            r = Convert.ToByte(colorCode.Substring(2, 2), 16);
            g = Convert.ToByte(colorCode.Substring(4, 2), 16);
            b = Convert.ToByte(colorCode.Substring(6, 2), 16);
        }
        else if (colorCode.Length == 6)
        {
            r = Convert.ToByte(colorCode.Substring(0, 2), 16);
            g = Convert.ToByte(colorCode.Substring(2, 2), 16);
            b = Convert.ToByte(colorCode.Substring(4, 2), 16);
        }
        else
        {
            throw new ArgumentException("Invalid color code");
        }

        var c = Windows.UI.Color.FromArgb(a, r, g, b);
        return c;
    }
}