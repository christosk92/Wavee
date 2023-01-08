// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UI.Xaml;
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
using System.Windows.Forms;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.UI.Items;
using Eum.UI.ViewModels;
using Eum.UI.ViewModels.Artists;
using Eum.UI.ViewModels.Playback;
using Microsoft.UI.Xaml.Controls;
using UserControl = Microsoft.UI.Xaml.Controls.UserControl;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UI.WinUI.Controls
{
    public sealed partial class BottomPlayerControl : UserControl
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(PlaybackViewModel), typeof(BottomPlayerControl), new PropertyMetadata(default(PlaybackViewModel)));
        public static readonly DependencyProperty CommandBarProperty = DependencyProperty.Register(nameof(CommandBar), typeof(CommandBar), typeof(BottomPlayerControl), new PropertyMetadata(default(CommandBar)));

        public BottomPlayerControl()
        {
            this.InitializeComponent();
        }

        public PlaybackViewModel ViewModel
        {
            get => (PlaybackViewModel) GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public CommandBar CommandBar
        {
            get => (CommandBar) GetValue(CommandBarProperty);
            set => SetValue(CommandBarProperty, value);
        }

 
        private async void ListViewBase_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var clickedDevice = e.ClickedItem as RemoteDevice;

            await ViewModel.SwitchRemoteDevice(clickedDevice.DeviceId);
        }

    
    }

}
