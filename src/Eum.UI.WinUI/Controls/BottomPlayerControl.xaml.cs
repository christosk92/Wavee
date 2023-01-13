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
            get => (PlaybackViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public CommandBar CommandBar
        {
            get => (CommandBar)GetValue(CommandBarProperty);
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
                        await Ioc.Default.GetRequiredService<IPlaybackService>().SetPosition((long)val);
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

        private async void ShuffleBtn_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await Ioc.Default.GetRequiredService<IPlaybackService>()
                .ToggleShuffle((sender as ToggleButton).IsChecked ?? false);
        }

        private async void RepeatBtn_Tapped(object sender, TappedRoutedEventArgs e)
        {
            /*
             *   None = 0,
               Context = 1,
               Track = 2
             */
            var nextRepeat = (RepeatMode)((int)(ViewModel.RepeatMode + 1) % 3);

            await Ioc.Default.GetRequiredService<IPlaybackService>()
                .SetRepeatMode(nextRepeat);
        }
        private async void Pause_Play_Btn_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var paused = ViewModel.IsPaused;
            if (paused)
            {
                await Ioc.Default.GetRequiredService<IPlaybackService>()
                    .Resume();
            }
            else
            {
                await Ioc.Default.GetRequiredService<IPlaybackService>()
                    .Pause();
            }
        }
        private async void Prev_Btn_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await Ioc.Default.GetRequiredService<IPlaybackService>()
                .SkipPrevious();
        }

        private async void Next_Btn_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await Ioc.Default.GetRequiredService<IPlaybackService>()
                .SkipNext();
        }
        public string GetGlyphForRepeat(RepeatMode repeatMode)
        {
            switch (repeatMode)
            {
                case RepeatMode.None:
                case RepeatMode.Context:
                    return "\uE1CD";
                case RepeatMode.Track:
                    return "\uE1CC";
                default:
                    throw new ArgumentOutOfRangeException(nameof(repeatMode), repeatMode, null);
            }
        }

        public string GetGlyphForPause(bool b)
        {
            return b ? "\uE102" : "\uE103";
        }

        private async void VolumeSlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (Math.Abs(e.NewValue - ViewModel.Volume) > 0.01)
            {
                await Ioc.Default.GetRequiredService<IPlaybackService>()
                    .Volume(e.NewValue / 100);
            }
        }
        private double _previousVolumeVal = -1;

        private async void VolumeSlider_OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            var val = VolumeSlider.Value;

            if (Math.Abs(_previousVolumeVal - val) > 0.1)
            {
                await Ioc.Default.GetRequiredService<IPlaybackService>()
                    .Volume(val / 100);
                _previousVolumeVal = val;
            }
        }

        private async void VolumeSlider_OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            var val = VolumeSlider.Value;

            if (Math.Abs(_previousVolumeVal - val) > 0.1)
            {
                await Ioc.Default.GetRequiredService<IPlaybackService>()
                    .Volume(val / 100);
                _previousVolumeVal = val;
            }
        }
    }

}
