using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Eum.UI.ViewModels.Playlists;
using Microsoft.Toolkit.Uwp.UI.Behaviors;

namespace Eum.UWP.Behaviors
{
    public class PlayingTrackPointerBehavior : BehaviorBase<Control>
    {
        protected override void OnAssociatedObjectLoaded()
        {
            base.OnAssociatedObjectLoaded();
            AssociatedObject.PointerEntered += AssociatedObjectOnPointerEntered;
            AssociatedObject.PointerExited += AssociatedObjectOnPointerExited;
            AssociatedObjectOnPointerExited(null, null);
            if (AssociatedObject.DataContext is IIsPlaying isPlaying)
            {
                isPlaying.IsPlayingChanged += IsPlayingOnIsPlayingChanged;
            }
        }


        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (AssociatedObject != null)
            {
                AssociatedObject.PointerEntered -= AssociatedObjectOnPointerEntered;
                AssociatedObject.PointerExited -= AssociatedObjectOnPointerExited;

                if (AssociatedObject.DataContext is IIsPlaying isPlaying)
                {
                    isPlaying.IsPlayingChanged -= IsPlayingOnIsPlayingChanged;
                }
            }
        }

        private void AssociatedObjectOnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (IsPlaying())
            {
                try
                {
                    VisualStateManager.GoToState(AssociatedObject, "Playing", true);
                }
                catch (Exception x)
                {
                    
                }
            }
            else
            {
                VisualStateManager.GoToState(AssociatedObject, "Normal", true);
            }
        }

        private void AssociatedObjectOnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (IsPlaying())
            {
                VisualStateManager.GoToState(AssociatedObject, "HoverPlaying", true);
            }
            else
            {
                VisualStateManager.GoToState(AssociatedObject, "Hover", true);
            }
        }

        public bool IsPlaying()
        {
            if (AssociatedObject.DataContext is IIsPlaying isPlaying)
                return isPlaying.IsPlaying();
            return false;
        }
        private void IsPlayingOnIsPlayingChanged(object sender, bool e)
        {
            AssociatedObjectOnPointerExited(null, null);
        }
    }
}
