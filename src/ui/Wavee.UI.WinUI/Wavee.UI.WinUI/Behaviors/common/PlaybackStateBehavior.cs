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

namespace Wavee.UI.WinUI.Behaviors.common;

public class PlaybackStateBehavior : BehaviorBase<Microsoft.UI.Xaml.Controls.ItemsView>
{
    private DispatcherQueue? _dispatcherQueue;
    public static readonly DependencyProperty ProfileProperty = DependencyProperty.Register(nameof(Profile),
        typeof(IWaveeUIAuthenticatedProfile), typeof(PlaybackStateBehavior),
        new PropertyMetadata(default(IWaveeUIAuthenticatedProfile), PropertyChangedCallback));

    private long _token;
    private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var x = (PlaybackStateBehavior)d;
        x.OnChanged(e);
    }

    protected override void OnAssociatedObjectLoaded()
    {
        base.OnAssociatedObjectLoaded();
        _dispatcherQueue = this.AssociatedObject.DispatcherQueue;
    }

    protected override void OnAssociatedObjectUnloaded()
    {
        base.OnAssociatedObjectUnloaded();
        _dispatcherQueue = null;
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        if (Profile is not null)
        {
            ProfileOnPlaybackStateChanged(Profile, Profile.LatestPlaybackState);
            Profile.PlaybackStateChanged -= ProfileOnPlaybackStateChanged;
            Profile.PlaybackStateChanged += ProfileOnPlaybackStateChanged;
        }

        _token = AssociatedObject.RegisterPropertyChangedCallback(ItemsView.ItemsSourceProperty, (sender, dp) =>
        {
            if (Profile is not null)
            {
                ProfileOnPlaybackStateChanged(Profile, Profile.LatestPlaybackState);
            }
        });
    }

    private void ProfileOnPlaybackStateChanged(object sender, WaveeUIPlaybackState e)
    {
        if (_dispatcherQueue is null) return;


        void DoStuff()
        {
            var enumerable = (IEnumerable)this.AssociatedObject.ItemsSource;
            foreach (var item in enumerable.Cast<WaveeTrackViewModel>())
            {
                if (item.Is(e.Item, e.Uid))
                {
                    Debug.WriteLine($"{e.Item.Name} is playing!");
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

        if (Profile is not null)
        {
            Profile.PlaybackStateChanged -= ProfileOnPlaybackStateChanged;
        }

        AssociatedObject.UnregisterPropertyChangedCallback(ItemsView.ItemsSourceProperty, _token);
    }

    public IWaveeUIAuthenticatedProfile Profile
    {
        get => (IWaveeUIAuthenticatedProfile)GetValue(ProfileProperty);
        set => SetValue(ProfileProperty, value);
    }

    private void OnChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is IWaveeUIAuthenticatedProfile oldProfile)
        {
            oldProfile.PlaybackStateChanged -= ProfileOnPlaybackStateChanged;
        }

        if (e.NewValue is IWaveeUIAuthenticatedProfile newProfile)
        {
            ProfileOnPlaybackStateChanged(newProfile, newProfile.LatestPlaybackState);
            newProfile.PlaybackStateChanged += ProfileOnPlaybackStateChanged;
        }
    }

}
