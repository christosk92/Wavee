using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Wavee.Spotify.Infrastructure.Remote.Contracts;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Components.Player
{
    public sealed partial class PlayingOnRemoteDeviceControl : UserControl
    {
        public static readonly DependencyProperty DeviceProperty = DependencyProperty.Register(nameof(Device), typeof(SpotifyRemoteDeviceInfo), typeof(PlayingOnRemoteDeviceControl), new PropertyMetadata(default(SpotifyRemoteDeviceInfo)));

        public PlayingOnRemoteDeviceControl()
        {
            this.InitializeComponent();
        }

        public SpotifyRemoteDeviceInfo Device
        {
            get => (SpotifyRemoteDeviceInfo)GetValue(DeviceProperty);
            set => SetValue(DeviceProperty, value);
        }
    }
}
