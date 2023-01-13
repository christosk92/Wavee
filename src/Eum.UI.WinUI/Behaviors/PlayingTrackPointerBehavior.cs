using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.UI;
using CommunityToolkit.WinUI.UI.Behaviors;
using Eum.UI.ViewModels;
using Eum.UI.ViewModels.Playlists;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Xaml.Interactivity;

namespace Eum.UI.WinUI.Behaviors
{
    public class PlayingTrackPointerBehavior : BehaviorBase<Control>
    {
        private bool _subscribed;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PointerEntered += AssociatedObjectOnPointerEntered;
            AssociatedObject.PointerExited += AssociatedObjectOnPointerExited;

            AssociatedObjectOnPointerExited(null, null);
        }

        protected override void OnAssociatedObjectLoaded()
        {
            base.OnAssociatedObjectLoaded();
            AssociatedObjectOnPointerExited(null, null);

            if (IsPlaying())
            {
                var findAscendant = AssociatedObject.FindAscendant<ListView>();
                var listBehavior =
                    Interaction.GetBehaviors(findAscendant)
                        .First(a => a is IsPlayingListBehavior) as IsPlayingListBehavior;
                listBehavior.PreviousPlayingContainers.Add(AssociatedObject);
            }
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.PointerEntered -= AssociatedObjectOnPointerEntered;
                AssociatedObject.PointerExited -= AssociatedObjectOnPointerExited;
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
                return Ioc.Default.GetRequiredService<MainViewModel>()
                    .PlaybackViewModel!.Id == isPlaying.Id;
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
