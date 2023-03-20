using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;
using Microsoft.UI.Dispatching;
using Wavee.UI.ViewModels.Playback;
using Wavee.UI.ViewModels.User;
using UserControl = Microsoft.UI.Xaml.Controls.UserControl;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Wavee.UI.WinUI.Controls
{
    public sealed partial class BottomPlayerControl : UserControl
    {
        private Guid _callback;
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel),
            typeof(PlaybackViewModel), typeof(BottomPlayerControl),
            new PropertyMetadata(default(PlaybackViewModel), PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var bpc = (BottomPlayerControl)d;
            if (e.OldValue is PlaybackViewModel vold)
            {
                vold.UnregisterPositionCallback(bpc._callback);
            }
            if (e.NewValue is PlaybackViewModel v)
            {
                bpc._callback = bpc.RegisterPositionCallback(100);
            }
        }


        public BottomPlayerControl()
        {
            this.InitializeComponent();
        }

        public PlaybackViewModel ViewModel
        {
            get => (PlaybackViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public UserViewModel User
        {
            get => (UserViewModel)GetValue(UserProperty);
            set => SetValue(UserProperty, value);
        }

        private void Callback(ulong obj)
        {
            if (dragStarted)
                return;
            if (DurationSlider?.DispatcherQueue != null)
            {
                DurationSlider.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
                {
                    DurationSlider.Value = obj;
                    var timeSpan = TimeSpan.FromMilliseconds(obj);
                    DurationBlock.Text = $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
                });
            }
        }
        private Guid RegisterPositionCallback(ulong minDiff)
        {
            return ViewModel.RegisterPositionCallback(minDiff, Callback);
        }

        private bool dragStarted;
        public static readonly DependencyProperty UserProperty = DependencyProperty.Register(nameof(User), typeof(UserViewModel), typeof(BottomPlayerControl), new PropertyMetadata(default(UserViewModel)));

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



        private void PauseButtonLoaded(object sender, RoutedEventArgs e)
        {
            var bOpen = (sender as Button);
            bOpen.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(bOpen_PointerPressed), true);
            bOpen.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(bOpen_PointerReleased), true);
        }

        private void bOpen_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            AnimateButtonScale(sender, 1);
        }

        private void bOpen_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            AnimateButtonScale(sender, 0.95);
        }
        private void AnimateButtonScale(object sender, double targetScale)
        {
            if (sender is Button button)
            {
                var grid = VisualTreeHelper.GetChild(button, 0) as Grid;
                if (grid != null)
                {
                    var transform = grid.RenderTransform as CompositeTransform;
                    if (transform != null)
                    {
                        var scaleXAnimation = new DoubleAnimation
                        {
                            To = targetScale,
                            Duration = TimeSpan.FromMilliseconds(100),
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                        };
                        var scaleYAnimation = new DoubleAnimation
                        {
                            To = targetScale,
                            Duration = TimeSpan.FromMilliseconds(100),
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                        };
                        Storyboard.SetTarget(scaleXAnimation, transform);
                        Storyboard.SetTarget(scaleYAnimation, transform);
                        Storyboard.SetTargetProperty(scaleXAnimation, "ScaleX");
                        Storyboard.SetTargetProperty(scaleYAnimation, "ScaleY");

                        var storyboard = new Storyboard();
                        storyboard.Children.Add(scaleXAnimation);
                        storyboard.Children.Add(scaleYAnimation);
                        storyboard.Begin();
                        ShadowC.Opacity = targetScale < 1 ? 0.3 : 0.5;
                    }
                }
            }
        }
    }
}
