using System.Linq;
using CommunityToolkit.WinUI.UI.Behaviors;
using Microsoft.UI.Xaml.Controls;
using Wavee.Interfaces.Models;
using Wavee.UI.ViewModels.Playback;
using Wavee.UI.ViewModels.Track;
using Wavee.UI.WinUI.Controls;

namespace Wavee.UI.WinUI.Behaviors
{
    public class IsPlayingListViewBehavior : BehaviorBase<ListViewBase>
    {
        private TrackControlContainer? _lastItem;
        protected override void OnAttached()
        {
            base.OnAttached();
            PlaybackViewModel.Instance!.PlayingItemChanged += InstanceOnPlayingItemChanged;
            PlaybackViewModel.Instance!.PauseChanged += OnPauseChanged;
            AssociatedObject.ContainerContentChanging += OnContainerContentChanging;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            PlaybackViewModel.Instance!.PlayingItemChanged -= InstanceOnPlayingItemChanged;
            PlaybackViewModel.Instance!.PauseChanged -= OnPauseChanged;
            AssociatedObject.ContainerContentChanging -= OnContainerContentChanging;
        }

        protected override void OnAssociatedObjectLoaded()
        {
            base.OnAssociatedObjectLoaded();
            InstanceOnPlayingItemChanged(null, PlaybackViewModel.Instance!.PlayingItem);
        }


        private void OnPauseChanged(object sender, bool e)
        {
            if (_lastItem != null)
            {
                _lastItem.IsPlaying = true;
                _lastItem.IsPaused = e;
            }
        }

        private void InstanceOnPlayingItemChanged(object sender, IPlayableItem e)
        {
            //find the next interesting item if applicable
            //all container items in the listview should be of type TrackControlContainer
            var lastViewModel = AssociatedObject.Items
                .FirstOrDefault(a => a is TrackViewModel v && v.Track.Equals(e)) as TrackViewModel;

            var container = AssociatedObject.ContainerFromItem(lastViewModel) as ListViewItem;
            if (_lastItem != null && container is not null)
            {
                _lastItem.IsPlaying = false;
                _lastItem = null;
            }

            if (container?.ContentTemplateRoot is TrackControlContainer tcc)
            {
                tcc.IsPlaying = true;
                //store the item so we can reset it later
                _lastItem = tcc;
            }
        }
        private void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.Item is TrackViewModel v)
            {
                // && v.Track.Equals(PlaybackViewModel.Instance?.PlayingItem) && _lastItem == null
                if (!args.InRecycleQueue)
                {
                    if (v.Track.Equals(PlaybackViewModel.Instance?.PlayingItem))
                    {
                        var container = args.ItemContainer as ListViewItem;
                        if (_lastItem != null)
                        {
                            _lastItem.IsPlaying = false;
                            _lastItem = null;
                        }

                        if (container?.ContentTemplateRoot is TrackControlContainer tcc)
                        {
                            tcc.IsPlaying = true;
                            //store the item so we can reset it later
                            _lastItem = tcc;
                        }
                    }
                }
                else
                {
                    if (!v.Track.Equals(PlaybackViewModel.Instance?.PlayingItem))
                    {
                        var container = args.ItemContainer as ListViewItem;
                        if (container?.ContentTemplateRoot is TrackControlContainer tcc)
                        {
                            tcc.IsPlaying = false;
                        }
                    }
                }
            }
        }
    }
}