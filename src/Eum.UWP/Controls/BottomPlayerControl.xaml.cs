// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Eum.UI.ViewModels.Playback;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UWP.Controls
{
    public sealed partial class BottomPlayerControl : UserControl
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(PlaybackViewModel), typeof(BottomPlayerControl), new PropertyMetadata(default(PlaybackViewModel)));

        public BottomPlayerControl()
        {
            this.InitializeComponent();
        }

        public PlaybackViewModel ViewModel
        {
            get => (PlaybackViewModel) GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        private async void ListViewBase_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var clickedDevice = e.ClickedItem as RemoteDevice;

            await ViewModel.SwitchRemoteDevice(clickedDevice.DeviceId);
        }
    }
}
