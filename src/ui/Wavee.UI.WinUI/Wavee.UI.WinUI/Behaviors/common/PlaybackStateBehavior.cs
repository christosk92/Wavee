using System.Collections;
using System.Diagnostics;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.Xaml.Interactivity;
using System.Threading.Tasks;
using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Providers;
using Wavee.UI.ViewModels.Artist;
using Wavee.UI.ViewModels.NowPlaying;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Wavee.UI.WinUI.Behaviors.common;

public sealed class PlaybackStateBehaviorItemsView : PlaybackStateBehavior<ItemsView>
{
    public override IEnumerable ItemsSource => (IEnumerable)this.AssociatedObject.ItemsSource;
}
public sealed class PlaybackStateBehaviorItemsRepeater : PlaybackStateBehavior<ItemsRepeater>
{
    public override IEnumerable ItemsSource => (IEnumerable)this.AssociatedObject.ItemsSource;
}
public abstract class PlaybackStateBehavior<T> : BehaviorBase<T> where T : FrameworkElement
{
    private DispatcherQueue? _dispatcherQueue;
    private long _token;
    protected override void OnAssociatedObjectLoaded()
    {
        base.OnAssociatedObjectLoaded();
        _dispatcherQueue = this.AssociatedObject.DispatcherQueue;
    }
    public abstract IEnumerable ItemsSource { get; }

    protected override void OnAssociatedObjectUnloaded()
    {
        base.OnAssociatedObjectUnloaded();
        _dispatcherQueue = null;
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        ProfileOnPlaybackStateChanged(null, NowPlayingViewModel.Instance.LatestPlaybackState);
        NowPlayingViewModel.Instance.PlaybackStateChanged -= ProfileOnPlaybackStateChanged;
        NowPlayingViewModel.Instance.PlaybackStateChanged += ProfileOnPlaybackStateChanged;

        _token = AssociatedObject.RegisterPropertyChangedCallback(ItemsView.ItemsSourceProperty, (sender, dp) =>
        {
            ProfileOnPlaybackStateChanged(null, NowPlayingViewModel.Instance.LatestPlaybackState);
        });
    }

    private void ProfileOnPlaybackStateChanged(object sender, WaveeUIPlaybackState e)
    {
        if (_dispatcherQueue is null) return;


        void DoStuff()
        {
            var enumerable = ItemsSource;
            foreach (var item in enumerable.Cast<WaveePlayableItemViewModel>())
            {
                if (item.Is(e.Item, e.Uid, e.ContextId))
                {
                    Debug.WriteLine($"{item.Name} is playing!");
                    item.PlaybackState = e.IsPaused
                        ? WaveeUITrackPlaybackStateType.Paused
                        : WaveeUITrackPlaybackStateType.Playing;
                }
                else if (item.PlaybackState is not WaveeUITrackPlaybackStateType.NotPlaying)
                {
                    Debug.WriteLine($"{item.Name} WAS playing!");
                    item.PlaybackState = WaveeUITrackPlaybackStateType.NotPlaying;
                }
            }
        }

        if (_dispatcherQueue.HasThreadAccess) DoStuff();
        else _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, DoStuff);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        NowPlayingViewModel.Instance.PlaybackStateChanged -= ProfileOnPlaybackStateChanged;

        AssociatedObject.UnregisterPropertyChangedCallback(ItemsView.ItemsSourceProperty, _token);
    }

}

