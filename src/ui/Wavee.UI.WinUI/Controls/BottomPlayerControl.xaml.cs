using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Labs.WinUI.SizerBaseLocal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Wavee.Core.Contracts;
using Wavee.Core.Playback;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.ViewModels;

namespace Wavee.UI.WinUI.Controls;

public sealed partial class BottomPlayerControl : UserControl
{
    public static readonly DependencyProperty PlaybackProperty = DependencyProperty.Register(
        nameof(Playback), typeof(PlaybackViewModel<WaveeUIRuntime>), typeof(BottomPlayerControl),
        new PropertyMetadata(default(PlaybackViewModel<WaveeUIRuntime>), PropertyChangedCallback));

    private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var x = (BottomPlayerControl)d;
        x.RegiterPositionSlider();
    }

    private void RegiterPositionSlider()
    {
        Playback.RegisterPositionCallback(1000, c =>
        {
            if (_dragStarted) return;
            this.DispatcherQueue.TryEnqueue(() =>
            {
                ProgressSlider.Value = c;
                TimeElapsedElement.Text = FormatDuration(TimeSpan.FromMilliseconds(c));
            });
        });
    }

    public BottomPlayerControl()
    {
        this.InitializeComponent();
    }

    public PlaybackViewModel<WaveeUIRuntime> Playback
    {
        get => (PlaybackViewModel<WaveeUIRuntime>)GetValue(PlaybackProperty);
        set => SetValue(PlaybackProperty, value);
    }


    public Uri GetImageFor(ITrack track)
    {
        if (track is not null && track.Album.Artwork.Length > 0)
        {
            return new Uri(track.Album.Artwork[0].Url);
        }
        else
        {
            return new Uri("ms-appx:///Assets/album_placeholder.png");
        }
    }

    public IEnumerable<MetadataItem> TransformItemsForMetadata(ITrack track)
    {
        if (track is null) return Enumerable.Empty<MetadataItem>();

        return track.Artists.Select(c => new MetadataItem
        {
            Label = c.Name
        });
    }

    public string FormatDuration(ITrack track)
    {
        if (track is null) return "--:--";
        return FormatDuration(track.Duration);
    }

    private static string FormatDuration(TimeSpan duration)
    {
        var i = (int)duration.TotalMilliseconds;
        return $"{i / 60000:00}:{i / 1000 % 60:00}";
    }

    public string GetIconForPauseButton(bool paused)
    {
        return !paused
            ? "\uF8AE"
            : "\uF5B0";
    }

    private void ProgressSlider_OnLoaded(object sender, RoutedEventArgs e)
    {
        var slider = (Slider)sender;
        var thumb = slider.FindDescendant<Thumb>();
        thumb.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateRailsX;
        thumb.ManipulationStarted += ThumbOnManipulationStarted;
        thumb.ManipulationCompleted += ThumbOnManipulationCompleted;
    }

    private bool _dragStarted;
    private async void ThumbOnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
    {
        var to = ProgressSlider.Value;
        await Playback.SeekToAsync(to);
        _dragStarted = false;
    }

    private void ThumbOnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
    {
        _dragStarted = true;
    }

    private void ProgressSlider_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
    {
        _dragStarted = true;
    }

    private async void ProgressSlider_OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
    {
        var to = ProgressSlider.Value;
        await Playback.SeekToAsync(to);
        _dragStarted = false;
    }

    private async void ProgressSlider_OnTapped(object sender, TappedRoutedEventArgs e)
    {
        var to = ProgressSlider.Value;
        await Playback.SeekToAsync(to);
        _dragStarted = false;
    }

    public bool GetRepeatStateTrue(RepeatState repeatState)
    {
        return repeatState > RepeatState.None;
    }

    public string GetRepeatStateIcon(RepeatState repeatState)
    {
        return repeatState switch
        {
            RepeatState.None or RepeatState.Context => "\uE1CD",
            RepeatState.Track => "\uE1CC",
            _ => throw new ArgumentOutOfRangeException(nameof(repeatState), repeatState, null)
        };
    }

    public string GetVolumeIcon(double d)
    {
        return (d / 100) switch
        {
            < 0.1 when CloseToZero(d) => "\uE198",
            < 0.33 => "\uE993",
            < 0.66 => "\uE994",
            _ => "\uE995"
        };
    }

    private static bool CloseToZero(double d)
    {
        return Math.Abs(d) < 0.0001;
    }
}