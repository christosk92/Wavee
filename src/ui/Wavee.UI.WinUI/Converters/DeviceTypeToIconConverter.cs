using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Wavee.Domain.Playback;

namespace Wavee.UI.WinUI.Converters;

public sealed class DeviceTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return ((RemoteDeviceType)value) switch
        {
            RemoteDeviceType.Unknown => Create('\uE9CE'),
            RemoteDeviceType.Computer => Create('\uE7F8'),
            RemoteDeviceType.Tablet => Create('\uE70A'),
            RemoteDeviceType.Smartphone => Create('\uE8EA'),
            RemoteDeviceType.Speaker => Create('\uE7F5'),
            RemoteDeviceType.Tv => Create('\uE7F4'),
            RemoteDeviceType.Avr => Create('\uE7F5'),
            RemoteDeviceType.Stb => Create('\uE7F5'),
            RemoteDeviceType.AudioDongle => Create('\uECF1'),
            RemoteDeviceType.GameConsole => Create('\uE990'),
            RemoteDeviceType.CastVideo => Create('\uEC15'),
            RemoteDeviceType.CastAudio => Create('\uEC15'),
            RemoteDeviceType.Automobile => Create('\uE804'),
            RemoteDeviceType.Smartwatch => Create('\uE916'),
            RemoteDeviceType.Chromebook => Create('\uE7F8'),
            RemoteDeviceType.UnknownSpotify => Create('\uE9CE'),
            RemoteDeviceType.CarThing => Create('\uE804'),
            RemoteDeviceType.Observer => Create('\uE9CE'),
            RemoteDeviceType.HomeThing => Create('\uEC26'),
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }


    private static IconElement Create(char glyph)
    {
        return new FontIcon
        {
            FontFamily = SegoeFluentIcons,
            Glyph = glyph.ToString()
        };
    }
    private static FontFamily SegoeFluentIcons { get; } =  (FontFamily)Microsoft.UI.Xaml.Application.Current.Resources["FluentIcons"]!;
}