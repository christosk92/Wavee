using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Animations;
using Microsoft.Toolkit.Uwp.UI.Behaviors;

namespace Eum.UWP.Behaviors
{
    class ImageShadowBehavior : BehaviorBase<UserControl>
    {
        public static readonly DependencyProperty ControlNameProperty = DependencyProperty.Register(nameof(ControlName), typeof(string), typeof(ImageShadowBehavior), new PropertyMetadata(default(string)));

        protected override void OnAssociatedObjectLoaded()
        {
            base.OnAssociatedObjectLoaded();
            AssociatedObject.PointerEntered += AssociatedObjectOnPointerEntered;
            AssociatedObject.PointerExited += AssociatedObjectOnPointerExited;
        }


        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (AssociatedObject != null)
            {
                AssociatedObject.PointerEntered -= AssociatedObjectOnPointerEntered;
                AssociatedObject.PointerExited -= AssociatedObjectOnPointerExited;
            }

            _control = null;
        }

        private void AssociatedObjectOnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            _control ??= AssociatedObject.FindDescendant<FrameworkElement>(a => a.Name == ControlName);

            if (_control == null) return;
            var builder =
                AnimationBuilder.Create()
                    .Scale(1, 1.05, duration: TimeSpan.FromMilliseconds(150), easingType: EasingType.Quartic,
                        easingMode: EasingMode.EaseOut);
            var opacity = new OpacityDropShadowAnimation
            {
                From = 1,
                To = 0
            }.AppendToBuilder(builder, _control);
            builder.Start(_control);
        }

        private void AssociatedObjectOnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _control ??= AssociatedObject.FindDescendant<FrameworkElement>(a => a.Name == ControlName);
            if (_control == null) return;
            var builder =
                AnimationBuilder.Create()
                    .Scale(1.05, 1, duration: TimeSpan.FromMilliseconds(300), easingType: EasingType.Quartic,
                        easingMode: EasingMode.EaseOut);
            var opacity = new OpacityDropShadowAnimation
            {
                From = 0,
                To = 1
            }.AppendToBuilder(builder, _control);
            builder.Start(_control);
        }

        public string ControlName
        {
            get => (string) GetValue(ControlNameProperty);
            set => SetValue(ControlNameProperty, value);
        }

        private UIElement? _control;
    }
}
