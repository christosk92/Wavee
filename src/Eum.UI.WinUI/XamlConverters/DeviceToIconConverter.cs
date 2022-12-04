using System;
using Eum.Spotify.connectstate;
using Microsoft.UI.Xaml.Data;

namespace Eum.UI.WinUI.XamlConverters
{
    public class DeviceToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DeviceType type)
            {
                switch (type)
                {
                    case DeviceType.Computer:
                        return "\xE7F8";
                    case DeviceType.Smartphone:
                        return "\xE8EA";
                    case DeviceType.Speaker:
                        return "\xE7F5";
                    case DeviceType.Tablet:
                        return "\xEBFC";
                    case DeviceType.Tv:
                        return "\xE7F4";
                    case DeviceType.Stb:
                    case DeviceType.Avr:
                        return "\xE7F3";
                    case DeviceType.AudioDongle:
                        return "\xF191";
                    case DeviceType.GameConsole:
                        return "\xE990";
                    case DeviceType.CastVideo:
                        return "\xEC16";
                    case DeviceType.CastAudio:
                        return "\xEC15";
                    case DeviceType.Automobile:
                        return "\xE804";
                }
            }
            return "\xE9CE";
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

