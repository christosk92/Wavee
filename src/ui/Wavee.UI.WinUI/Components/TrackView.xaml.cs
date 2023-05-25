using System;
using System.Windows.Input;
using AnimatedVisuals;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Wavee.Core.Contracts;
using Wavee.Core.Ids;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.Models;
using Wavee.UI.ViewModels;

namespace Wavee.UI.WinUI.Components;

public sealed partial class TrackView : UserControl
{
    public static readonly DependencyProperty ViewProperty = DependencyProperty.Register(nameof(View), typeof(object), typeof(TrackView), new PropertyMetadata(default(object)));
    public static readonly DependencyProperty IndexProperty = DependencyProperty.Register(nameof(Index), typeof(int), typeof(TrackView), new PropertyMetadata(default(int)));
    public static readonly DependencyProperty ShowImageProperty = DependencyProperty.Register(nameof(ShowImage), typeof(bool), typeof(TrackView), new PropertyMetadata(default(bool)));
    public static readonly DependencyProperty ImageUrlProperty =
        DependencyProperty.Register(nameof(ImageUrl),
            typeof(string), typeof(TrackView),
            new PropertyMetadata(default(string?)));

    public static readonly DependencyProperty AlternatingRowColorProperty = DependencyProperty.Register(nameof(AlternatingRowColor), typeof(bool), typeof(TrackView), new PropertyMetadata(default(bool)));
    public static readonly DependencyProperty PlaybackStateProperty = DependencyProperty.Register(nameof(PlaybackState), typeof(TrackPlaybackState), typeof(TrackView), new PropertyMetadata(default(TrackPlaybackState), PlaybackStateChanged));
    public static readonly DependencyProperty IdProperty = DependencyProperty.Register(nameof(Id), typeof(AudioId), typeof(TrackView), new PropertyMetadata(default(AudioId), IdChanged));
    public static readonly DependencyProperty PlayCommandProperty = DependencyProperty.Register(nameof(PlayCommand), typeof(ICommand), typeof(TrackView), new PropertyMetadata(default(ICommand)));

    private static void IdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var x = (TrackView)d;
        x.HandleIdChange((AudioId)e.NewValue);
    }


    private static void PlaybackStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var x = (TrackView)d;

        if (e.NewValue is TrackPlaybackState tr && e.NewValue != e.OldValue)
        {
            x.ChangePlaybackState(tr);
        }
    }

    public TrackView()
    {
        this.InitializeComponent();
    }

    public object View
    {
        get => (object)GetValue(ViewProperty);
        set => SetValue(ViewProperty, value);
    }

    public int Index
    {
        get => (int)GetValue(IndexProperty);
        set => SetValue(IndexProperty, value);
    }

    public bool ShowImage
    {
        get => (bool)GetValue(ShowImageProperty);
        set => SetValue(ShowImageProperty, value);
    }

    public string ImageUrl
    {
        get => (string)GetValue(ImageUrlProperty);
        set => SetValue(ImageUrlProperty, value);
    }

    public bool AlternatingRowColor
    {
        get => (bool)GetValue(AlternatingRowColorProperty);
        set => SetValue(AlternatingRowColorProperty, value);
    }

    public TrackPlaybackState PlaybackState
    {
        get => (TrackPlaybackState)GetValue(PlaybackStateProperty);
        set => SetValue(PlaybackStateProperty, value);
    }

    public AudioId Id
    {
        get => (AudioId)GetValue(IdProperty);
        set => SetValue(IdProperty, value);
    }

    public ICommand PlayCommand
    {
        get => (ICommand)GetValue(PlayCommandProperty);
        set => SetValue(PlayCommandProperty, value);
    }

    private void HandleIdChange(AudioId id)
    {
        if (ShellViewModel<WaveeUIRuntime>.Instance.Playback.CurrentTrack?.Id.Equals(id) is true)
        {
            PlaybackState = ShellViewModel<WaveeUIRuntime>.Instance.Playback.Paused
                ? TrackPlaybackState.Paused
                : TrackPlaybackState.Playing;
        }
        else
        {
            PlaybackState = TrackPlaybackState.None;
        }
    }
    private void ChangePlaybackState(TrackPlaybackState state)
    {
        switch (state)
        {
            case TrackPlaybackState.None:
                VisualStateManager.GoToState(this, "NoPlayback", true);
                break;
            case TrackPlaybackState.Playing:
                VisualStateManager.GoToState(this, "PlayingPlayback", true);
                // PlaybackViewContent.Content = new AnimatedVisualPlayer
                // {
                //     Source = new Equaliser(),
                //     AutoPlay = true,
                //     PlaybackRate = 2.0
                // };
                break;
            case TrackPlaybackState.Paused:
                VisualStateManager.GoToState(this, "PausedPlayback", true);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    private void RegisterPlaybackEvents()
    {
        ShellViewModel<WaveeUIRuntime>.Instance.Playback.PauseChanged += PlaybackOnPauseChanged;
        ShellViewModel<WaveeUIRuntime>.Instance.Playback.CurrentTrackChanged += PlaybackOnCurrentTrackChanged;
    }
    private void UnregisterPlaybackEvents()
    {
        ShellViewModel<WaveeUIRuntime>.Instance.Playback.PauseChanged -= PlaybackOnPauseChanged;
        ShellViewModel<WaveeUIRuntime>.Instance.Playback.CurrentTrackChanged -= PlaybackOnCurrentTrackChanged;
    }
    private void PlaybackOnCurrentTrackChanged(object sender, ITrack e)
    {
        if (e is null)
        {
            PlaybackState = TrackPlaybackState.None;
            return;
        }

        if (e.Id.Equals(Id))
        {
            if (ShellViewModel<WaveeUIRuntime>.Instance.Playback.Paused)
            {

                PlaybackState = TrackPlaybackState.Paused;
            }
            else
            {
                PlaybackState = TrackPlaybackState.Playing;
            }
        }
        else
        {
            PlaybackState = TrackPlaybackState.None;
        }
    }
    private void PlaybackOnPauseChanged(object sender, bool e)
    {
        if (ShellViewModel<WaveeUIRuntime>.Instance.Playback.CurrentTrack is null)
        {
            PlaybackState = TrackPlaybackState.None;
            return;
        }

        if (ShellViewModel<WaveeUIRuntime>.Instance.Playback.CurrentTrack.Id.Equals(Id))
        {
            if (ShellViewModel<WaveeUIRuntime>.Instance.Playback.Paused)
            {

                PlaybackState = TrackPlaybackState.Paused;
            }
            else
            {
                PlaybackState = TrackPlaybackState.Playing;
            }
        }
    }

    public string FormatIndex(int i)
    {
        //if we have 1, we want 01.
        //2 should be 02.
        //3 should be 03.
        var index = i + 1;
        var str = index.ToString("D2");
        return $"{str}.";
    }

    public bool HasImage(string img, bool ifTrue)
    {
        return !string.IsNullOrEmpty(img) ? ifTrue : !ifTrue;
    }

    public Style GetStyleFor(int i)
    {
        return
            !AlternatingRowColor || (i % 2 == 0)
                ? (Style)Application.Current.Resources["EvenBorderStyle"]
                : (Style)Application.Current.Resources["OddBorderStyle"];
    }

    private void TrackView_OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (e.Pointer.PointerDeviceType is PointerDeviceType.Touch)
        {
            return;
        }
        switch (PlaybackState)
        {
            case TrackPlaybackState.None:
                VisualStateManager.GoToState(this, "NoPlaybackHover", true);
                break;
            case TrackPlaybackState.Playing:
                VisualStateManager.GoToState(this, "PlayingPlaybackHover", true);
                break;
            case TrackPlaybackState.Paused:
                VisualStateManager.GoToState(this, "PausedPlaybackHover", true);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void TrackView_OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (e.Pointer.PointerDeviceType is PointerDeviceType.Touch)
        {
            return;
        }
        switch (PlaybackState)
        {
            case TrackPlaybackState.None:
                VisualStateManager.GoToState(this, "NoPlayback", true);
                break;
            case TrackPlaybackState.Playing:
                // PlaybackViewContent.Content = new AnimatedVisualPlayer
                // {
                //     Source = new Equaliser(),
                //     AutoPlay = true,
                //     PlaybackRate = 2.0
                // };
                VisualStateManager.GoToState(this, "PlayingPlayback", true);
                break;
            case TrackPlaybackState.Paused:
                VisualStateManager.GoToState(this, "PausedPlayback", true);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void TrackView_OnUnloaded(object sender, RoutedEventArgs e)
    {
        UnregisterPlaybackEvents();
    }

    private void AnimatedVisualPlayerUnloaded(object sender, RoutedEventArgs e)
    {
        var p = (AnimatedVisualPlayer)sender;
        p.Stop();
    }

    private async void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
    {
        var p = (AnimatedVisualPlayer)sender;
        if (!p.IsPlaying)
        {
            await p.PlayAsync(0, 1, true);
        }
    }

    private void TrackView_OnLoaded(object sender, RoutedEventArgs e)
    {
        RegisterPlaybackEvents();
    }

    private void PauseButton_OnTapped(object sender, TappedRoutedEventArgs e)
    {
        ShellViewModel<WaveeUIRuntime>.Instance.Playback.ResumePauseCommand.Execute(null);
    }

    private void TrackView_OnTapped(object sender, TappedRoutedEventArgs e)
    {
        if (e.PointerDeviceType is PointerDeviceType.Touch or PointerDeviceType.Pen)
        {
            PlayCommand.Execute(Id);
        }
    }
}