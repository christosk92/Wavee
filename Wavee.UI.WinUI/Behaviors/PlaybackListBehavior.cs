using CommunityToolkit.WinUI.UI.Behaviors;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Wavee.Interfaces.Models;
using Wavee.UI.ViewModels.Playback;
using Wavee.UI.ViewModels.Track;
using Wavee.UI.WinUI.Controls;

namespace Wavee.UI.WinUI.Behaviors
{
    public class PlaybackListBehavior : BehaviorBase<ListViewBase>
    {
        protected override void OnAttached()
        {
            AssociatedObject.ContainerContentChanging += ListView_ContainerContentChanging;
            PlaybackViewModel.Instance.PlayingItemChanged += PlaybackViewModel_PlaybackEvent;
            PlaybackViewModel_PlaybackEvent(null, PlaybackViewModel.Instance.PlayingItem);
        }

        protected override void OnDetaching()
        {
            AssociatedObject.ContainerContentChanging -= ListView_ContainerContentChanging;
            PlaybackViewModel.Instance.PlayingItemChanged -= PlaybackViewModel_PlaybackEvent;
        }

        private void PlaybackViewModel_PlaybackEvent(object sender, IPlayableItem e)
        {
            var playbackViewModel = (PlaybackViewModel)sender;
            if (AssociatedObject != null)
            {
                for (int i = 0; i < AssociatedObject.Items.Count; i++)
                {
                    var container = (ListViewItem)AssociatedObject.ContainerFromIndex(i);
                    if (container == null) continue;

                    var trackControlContainer = FindVisualChild<TrackControlContainer>(container);
                    if (trackControlContainer == null) continue;

                    var trackViewModel = (TrackViewModel)AssociatedObject.Items[i];
                    trackControlContainer.IsPlaying = trackViewModel.Track.Equals(playbackViewModel.PlayingItem);
                }
            }
        }
        private static void ListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (!args.InRecycleQueue && args.ItemContainer != null)
            {
                var listView = (ListView)sender;

                if (PlaybackViewModel.Instance != null)
                {
                    var trackControlContainer = FindVisualChild<TrackControlContainer>(args.ItemContainer);
                    if (trackControlContainer != null)
                    {
                        var trackViewModel = (TrackViewModel)args.Item;
                        trackControlContainer.IsPlaying = trackViewModel.Track.Equals(PlaybackViewModel.Instance.PlayingItem);
                    }
                }
            }
        }
        private static T FindVisualChild<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                if (child != null && child is T)
                {
                    return (T)child;
                }

                T childItem = FindVisualChild<T>(child);
                if (childItem != null) return childItem;
            }
            return null;
        }
    }
}
