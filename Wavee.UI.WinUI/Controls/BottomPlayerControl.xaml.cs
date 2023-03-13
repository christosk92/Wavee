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
using Wavee.UI.ViewModels.Playback;
using Google.Protobuf.WellKnownTypes;
using Microsoft.UI.Dispatching;
using UserControl = Microsoft.UI.Xaml.Controls.UserControl;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Controls
{
    public sealed partial class BottomPlayerControl : UserControl
    {
        private Guid _callback;
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel),
            typeof(PlayerViewModel), typeof(BottomPlayerControl),
            new PropertyMetadata(default(PlayerViewModel), PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var bpc = (BottomPlayerControl)d;
            if (e.OldValue is PlayerViewModel vold)
            {
                vold.UnregisterPositionCallback(bpc._callback);
            }
            if (e.NewValue is PlayerViewModel v)
            {
                bpc._callback = bpc.RegisterPositionCallback(100);
            }
        }


        public BottomPlayerControl()
        {
            this.InitializeComponent();
        }

        public PlayerViewModel ViewModel
        {
            get => (PlayerViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }
        private void Callback(ulong obj)
        {
            if (dragStarted)
                return;
            DurationSlider.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
            {
                DurationSlider.Value = obj;
                var timeSpan = TimeSpan.FromMilliseconds(obj);
                DurationBlock.Text = $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
            });
        }
        private Guid RegisterPositionCallback(ulong minDiff)
        {
            return ViewModel.RegisterPositionCallback(minDiff, Callback);
        }

        private bool dragStarted;
        private async void Slider_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            var val = DurationSlider.Value;
            await ViewModel.Seek(val);
            dragStarted = false;
        }

        private void TimelineSlider_ManipulationStarting(object sender, ManipulationStartingRoutedEventArgs e)
        {
            dragStarted = true;
        }

        private async void DurationSlider_OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {

        }

        private async void DurationSlider_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            await ViewModel.Seek(DurationSlider.Value);
            dragStarted = false;
        }

        private void DurationSlider_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            dragStarted = true;
        }

        private async void DurationSlider_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (dragStarted)
            {
                await ViewModel.Seek(DurationSlider.Value);
                dragStarted = false;
            }
        }

        private void DurationSlider_OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {

        }

        private void DurationSlider_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            dragStarted = true;
        }

        private void DurationSlider_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            dragStarted = true;
        }
    }
}
