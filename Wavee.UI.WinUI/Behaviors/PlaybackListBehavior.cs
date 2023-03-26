using CommunityToolkit.WinUI.UI.Behaviors;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Wavee.Interfaces.Models;
using Wavee.UI.Interfaces.ViewModels;
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
            AssociatedObject.DoubleTapped += AssociatedObjectOnDoubleTapped;
            PlaybackViewModel.Instance.PlayingItemChanged += PlaybackViewModel_PlaybackEvent;
            PlaybackViewModel.Instance.PauseChanged += InstanceOnPauseChanged;
            PlaybackViewModel_PlaybackEvent(null, PlaybackViewModel.Instance.PlayingItem);
            InstanceOnPauseChanged(null, PlaybackViewModel.Instance.Paused);
        }

        protected override void OnDetaching()
        {
            AssociatedObject.ContainerContentChanging -= ListView_ContainerContentChanging;
            AssociatedObject.DoubleTapped -= AssociatedObjectOnDoubleTapped;
            PlaybackViewModel.Instance.PlayingItemChanged -= PlaybackViewModel_PlaybackEvent;
            PlaybackViewModel.Instance.PauseChanged -= InstanceOnPauseChanged;
        }
        private async void AssociatedObjectOnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var playItem = (TrackViewModel)(sender as ListViewBase).SelectedItem;
            if (playItem != null)
            {
                if (((ListViewBase)sender).DataContext is IPlayableViewModel p)
                {
                    await p.PlayCommand.ExecuteAsync(playItem);
                }
            }
        }

        private void InstanceOnPauseChanged(object sender, bool e)
        {
            var playbackViewModel = (PlaybackViewModel)sender;
            if (AssociatedObject != null)
            {
                for (int i = 0; i < AssociatedObject.Items.Count; i++)
                {
                    var container = (SelectorItem)AssociatedObject.ContainerFromIndex(i);
                    if (container == null) continue;

                    var trackControlContainer = FindVisualChild<TrackControlContainer>(container);
                    if (trackControlContainer == null) continue;
                    if (trackControlContainer.IsPlaying)
                    {
                        trackControlContainer.IsPaused = e;
                    }
                }
            }
        }
        private void PlaybackViewModel_PlaybackEvent(object sender, IPlayableItem e)
        {
            var playbackViewModel = (PlaybackViewModel)sender;
            if (AssociatedObject != null)
            {
                for (int i = 0; i < AssociatedObject.Items.Count; i++)
                {
                    var container = (SelectorItem)AssociatedObject.ContainerFromIndex(i);
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

                if (PlaybackViewModel.Instance != null)
                {
                    var trackControlContainer = FindVisualChild<TrackControlContainer>(args.ItemContainer);
                    if (trackControlContainer != null)
                    {
                        var trackViewModel = (TrackViewModel)args.Item;
                        trackControlContainer.IsPlaying = trackViewModel.Track.Equals(PlaybackViewModel.Instance.PlayingItem);
                        trackControlContainer.IsPaused = trackControlContainer.IsPlaying && PlaybackViewModel.Instance.Paused;
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
