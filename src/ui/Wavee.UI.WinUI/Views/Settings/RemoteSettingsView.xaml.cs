using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Eum.Spotify.connectstate;
using LanguageExt;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Settings
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RemoteSettingsView : Page
    {
        public RemoteSettingsView()
        {
            this.InitializeComponent();
            AvailableDeviceTypes = Seq(
                RemoteDeviceRecord.Computer,
                RemoteDeviceRecord.Smartphone,
                RemoteDeviceRecord.Tablet,
                RemoteDeviceRecord.TV
            );
        }

        public SettingsViewModel<WaveeUIRuntime> ViewModel => SettingsViewModel<WaveeUIRuntime>.Instance;
        public Seq<RemoteDeviceRecord> AvailableDeviceTypes { get; }

        private void DeviceTypeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.DeviceType = ((RemoteDeviceRecord)DeviceTypeComboBox.SelectedItem).DeviceType;
        }

        public object GetItemFrom(DeviceType deviceType)
        {
            return deviceType switch
            {
                DeviceType.Computer => RemoteDeviceRecord.Computer,
                DeviceType.Tablet => RemoteDeviceRecord.Tablet,
                DeviceType.Smartphone => RemoteDeviceRecord.Smartphone,
                DeviceType.Tv => RemoteDeviceRecord.TV,
            };
        }
    }

    public record RemoteDeviceRecord(DeviceType DeviceType, string Name, FontIcon Icon)
    {
        public static RemoteDeviceRecord Computer = new(DeviceType.Computer, "Computer", new FontIcon { FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"), Glyph = "\uE7F8" });
        public static RemoteDeviceRecord Tablet = new(DeviceType.Tablet, "Tablet", new FontIcon { FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"), Glyph = "\uE70A" });
        public static RemoteDeviceRecord Smartphone = new(DeviceType.Smartphone, "Phone", new FontIcon { FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"), Glyph = "\uE1C9" });
        public static RemoteDeviceRecord TV = new(DeviceType.Tv, "TV", new FontIcon { FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"), Glyph = "\uE7F4" });
    }
}
