using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Wavee.Core.Contracts;
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
}