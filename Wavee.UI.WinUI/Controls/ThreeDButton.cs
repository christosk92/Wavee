using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Controls
{
    public sealed class ThreeDButton : Button
    {
        public static readonly DependencyProperty JumpFactorProperty = DependencyProperty.Register(nameof(JumpFactor),
            typeof(double),
            typeof(ThreeDButton), new PropertyMetadata(0.9));

        public ThreeDButton()
        {
            this.DefaultStyleKey = typeof(ThreeDButton);
        }

        public double JumpFactor
        {
            get => (double)GetValue(JumpFactorProperty);
            set => SetValue(JumpFactorProperty, value);
        }

        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            AnimateButtonScale(this, JumpFactor);
            base.OnPointerPressed(e);
        }

        protected override void OnPointerReleased(PointerRoutedEventArgs e)
        {
            AnimateButtonScale(this, 1);
            base.OnPointerReleased(e);
        }

        protected override void OnPointerEntered(PointerRoutedEventArgs e)
        {
            base.OnPointerEntered(e);
            if (!e.IsGenerated && e.Pointer.PointerDeviceType == PointerDeviceType.Mouse && !e.Handled)
            {
                VisualStateManager.GoToState(this, "PointerOver", true);
            }
        }

        protected override void OnPointerExited(PointerRoutedEventArgs e)
        {
            base.OnPointerExited(e);
            VisualStateManager.GoToState(this, "Normal", true);
        }

        private void AnimateButtonScale(object sender, double targetScale)
        {
            if (sender is Button button)
            {
                var grid = VisualTreeHelper.GetChild(button, 0) as ContentPresenter;
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
                    }
                }
            }
        }
    }
}
