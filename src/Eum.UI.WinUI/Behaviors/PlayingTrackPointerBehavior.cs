using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.WinUI.UI.Behaviors;
using Eum.UI.ViewModels.Playlists;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Eum.UI.WinUI.Behaviors
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
                isPlaying.RegisterEvents();
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
                    isPlaying.UnregisterEvents();
                    isPlaying.IsPlayingChanged -= IsPlayingOnIsPlayingChanged;
                }
            }
        }

        private void AssociatedObjectOnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (IsPlaying())
            {
                VisualStateManager.GoToState(AssociatedObject, "Playing", true);
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
