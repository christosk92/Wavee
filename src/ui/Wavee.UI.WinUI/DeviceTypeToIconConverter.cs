using System;
using Eum.Spotify.connectstate;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Wavee.UI.WinUI;

public class DeviceTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        switch ((DeviceType)value)
        {
            case DeviceType.Chromebook:
            case DeviceType.Computer:
                return "\uE7F8";
            
            case DeviceType.Smartphone:
                return "\uE1C9";
            case DeviceType.CarThing:
            case DeviceType.Automobile:
                return "\uE804";
            case DeviceType.Tablet:
                return "\uE70A";
            case DeviceType.Tv:
                return "\uE7F4";
            case DeviceType.Speaker:
                return "\uE7F5";
            default:
                return "\uEA6C";
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}