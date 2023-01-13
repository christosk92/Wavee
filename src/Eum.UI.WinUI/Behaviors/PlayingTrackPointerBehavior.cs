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
        private bool _subscribed;
        private IIsPlaying _isPlaying;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.DataContextChanged += AssociatedObjectOnDataContextChanged;
            AssociatedObject.PointerEntered += AssociatedObjectOnPointerEntered;
            AssociatedObject.PointerExited += AssociatedObjectOnPointerExited;

            AssociatedObjectOnDataContextChanged(AssociatedObject, null);
            AssociatedObjectOnPointerExited(null, null);
        }

        private void AssociatedObjectOnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (sender.DataContext is IIsPlaying i && !_subscribed)
            {
                _subscribed = true;
                _isPlaying = i;
                _isPlaying.IsPlayingChanged += IsPlayingOnIsPlayingChanged;
            }
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.PointerEntered -= AssociatedObjectOnPointerEntered;
                AssociatedObject.PointerExited -= AssociatedObjectOnPointerExited;
                AssociatedObject.DataContextChanged -= AssociatedObjectOnDataContextChanged;
            }

            if (_isPlaying !=null)
            {
                _isPlaying.IsPlayingChanged -= IsPlayingOnIsPlayingChanged;
                _isPlaying = null;
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
