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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Forms;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.UI.Items;
using Eum.UI.Services;
using Eum.UI.ViewModels;
using Eum.UI.ViewModels.Artists;
using Eum.UI.ViewModels.Playback;
using Microsoft.UI.Xaml.Controls;
using UserControl = Microsoft.UI.Xaml.Controls.UserControl;
using NAudio.Wave;
using NAudio.Gui;
using System.Threading.Tasks;
using Nito.AsyncEx;

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


        private void PositionSlider_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            dragStarted = true;
        }
        private bool dragStarted = false;

        private void PositionSlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (!dragStarted)
            {
                // Do work
            }
        }

        private double _previousVal = -1;
        private AsyncLock _setPosLock = new AsyncLock();
        private async void PositionSlider_OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            var val = TimestampSlider.Value;
            using (await _setPosLock.LockAsync())
            {
                if (Math.Abs(_previousVal - val) > 0.1)
                {
                    await Ioc.Default.GetRequiredService<IPlaybackService>().SetPosition(val);
                    _previousVal = val;
                }

                dragStarted = false;
            }
        }

        private void PositionSlider_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            dragStarted = true;
        }

        private void PositionSlider_OnManipulationStarting(object sender, ManipulationStartingRoutedEventArgs e)
        {
            dragStarted = true;
        }

        private async void PositionSlider_OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            var val = TimestampSlider.Value;
            using (await _setPosLock.LockAsync())
            {
                if (dragStarted)
                {
                    if (Math.Abs(_previousVal - val) > 0.1)
                    {
                        await Ioc.Default.GetRequiredService<IPlaybackService>().SetPosition((long) val);
                        _previousVal = val;
                    }

                    dragStarted = false;
                }
            }
        }

        private void TimestampSlider_OnLoaded(object sender, RoutedEventArgs e)
        {
            ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        }

        private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.Timestamp))
            {
                if (!dragStarted)
                {
                    TimestampSlider.Value = ViewModel.Timestamp;
                }
            }
        }

        private void TimestampSlider_OnUnloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        }
    }

}
