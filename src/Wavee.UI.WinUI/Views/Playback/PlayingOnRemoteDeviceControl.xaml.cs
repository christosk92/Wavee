using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.Spotify.Infrastructure.Remote.Contracts;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Playback
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
