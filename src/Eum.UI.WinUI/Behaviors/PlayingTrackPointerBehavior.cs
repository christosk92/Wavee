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
        private bool _didSet = false;
        private bool _didSubscribe = false;
        protected override void OnAssociatedObjectLoaded()
        {
            base.OnAssociatedObjectLoaded();
            if (!_didSet)
            {
                _didSet = true;
                AssociatedObject.PointerEntered += AssociatedObjectOnPointerEntered;
                AssociatedObject.PointerExited += AssociatedObjectOnPointerExited;
                AssociatedObjectOnPointerExited(null, null);
            }

            if (!_didSubscribe)
            {
                if (AssociatedObject?.DataContext is IIsPlaying isPlaying)
                {
                    isPlaying.IsPlayingChanged += IsPlayingOnIsPlayingChanged;
                }
            }
            AssociatedObjectOnPointerExited(null, null);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                _didSet = true;
                AssociatedObject.PointerEntered += AssociatedObjectOnPointerEntered;
                AssociatedObject.PointerExited += AssociatedObjectOnPointerExited;
                AssociatedObjectOnPointerExited(null, null);
                if (AssociatedObject.DataContext is IIsPlaying isPlaying)
                {
                    isPlaying.IsPlayingChanged += IsPlayingOnIsPlayingChanged;
                    _didSubscribe = true;
                }
            }
        }

        protected override void OnAssociatedObjectUnloaded()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.PointerEntered -= AssociatedObjectOnPointerEntered;
                AssociatedObject.PointerExited -= AssociatedObjectOnPointerExited;

                if (AssociatedObject.DataContext is IIsPlaying isPlaying)
                {
                    isPlaying.IsPlayingChanged -= IsPlayingOnIsPlayingChanged;
                }
            }
            base.OnAssociatedObjectUnloaded();
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.PointerEntered -= AssociatedObjectOnPointerEntered;
                AssociatedObject.PointerExited -= AssociatedObjectOnPointerExited;

                if (AssociatedObject.DataContext is IIsPlaying isPlaying)
                {
                    isPlaying.IsPlayingChanged -= IsPlayingOnIsPlayingChanged;
                }
            }
            base.OnDetaching();
        }

        private void AssociatedObjectOnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (AssociatedObject != null)
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
        }

        private void AssociatedObjectOnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (AssociatedObject != null)
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
        }

        public bool IsPlaying()
        {
            if (AssociatedObject?.DataContext is IIsPlaying isPlaying)
                return isPlaying.IsPlaying();
            return false;
        }
        private void IsPlayingOnIsPlayingChanged(object sender, bool e)
        {
            if (e)
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
    }
}
