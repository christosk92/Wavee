using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Windows.UI;
using CommunityToolkit.WinUI.Helpers;
using CommunityToolkit.WinUI.UI;
using CommunityToolkit.WinUI.UI.Controls;
using CommunityToolkit.WinUI.UI.Media;
using DynamicData.Binding;
using Eum.Spotify.transfer;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using ReactiveUI;
using Wavee.Spotify.Infrastructure.Mercury.Models;
using Wavee.Spotify.Infrastructure.PrivateApi.Contracts.Response;
using Wavee.Spotify.Infrastructure.Remote.Contracts;
using Wavee.UI.Enums;
using Wavee.UI.ViewModels.Playback;
using AcrylicBrush = Microsoft.UI.Xaml.Media.AcrylicBrush;
using Image = Spotify.Metadata.Image;


namespace Wavee.UI.WinUI.Components.Player;

public sealed partial class PlayerControl : UserControl
{
    public static readonly DependencyProperty PlayerProperty = DependencyProperty.Register(nameof(Player), typeof(PlaybackViewModel), typeof(PlayerControl), new PropertyMetadata(default(PlaybackViewModel), PropertyChangedCallback));

    public PlayerControl()
    {
        this.InitializeComponent();
        TimeElapsedElement.Text = "--:--";
    }
    private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var x = (PlayerControl)d;
        x.RegiterPositionSlider();

        x.Player
            .WhenValueChanged(x => x.CurrentTrackColors)
            .Select(f =>
            {
                var actualTheme = (MainWindow.Instance.Content as FrameworkElement)!.ActualTheme;
                x.UpdateColors(actualTheme, f);
                return default(Unit);
            })
            .Subscribe();
    }
    private void RegiterPositionSlider()
    {
        Player.RegisterPositionCallback(1000, c =>
        {
            if (_dragStarted) return;
            this.DispatcherQueue.TryEnqueue(() =>
            {
                ProgressSlider.Value = c;
                TimeElapsedElement.Text = FormatDur(TimeSpan.FromMilliseconds(c));
            });
        });
    }

    public PlaybackViewModel Player
    {
        get => (PlaybackViewModel)GetValue(PlayerProperty);
        set => SetValue(PlayerProperty, value);
    }

    public Visibility IsLoadingToVisibility(bool b)
    {
        return b ? Visibility.Collapsed : Visibility.Visible;
    }

    public Visibility IsLoadingToVisibilityNegated(bool b)
    {
        return b ? Visibility.Visible : Visibility.Collapsed;
    }


    public string FormatDuration(TrackOrEpisode track)
    {
        if (track is null) return "--:--";
        return FormatDur(track.Duration);
    }

    private static string FormatDur(TimeSpan duration)
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
        await Player.SeekToAsync(to);
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
        await Player.SeekToAsync(to);
        _dragStarted = false;
    }

    private async void ProgressSlider_OnTapped(object sender, TappedRoutedEventArgs e)
    {
        var to = ProgressSlider.Value;
        await Player.SeekToAsync(to);
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

    public bool Negate(bool b)
    {
        return !b;
    }

    public Uri GetImageFor(TrackOrEpisode track)
    {
        if (track is not null && track.Group.ArtistName.Length > 0)
        {
            return new Uri(track.GetImage(Image.Types.Size.Default));
        }
        else
        {
            return new Uri("ms-appx:///Assets/icon_album_placeholder.png");
        }
    }

    public IEnumerable<MetadataItem> TransformItemsForMetadata(TrackOrEpisode track)
    {
        if (track is null) return Enumerable.Empty<MetadataItem>();

        return track.Artists.Select(c => new MetadataItem
        {
            Label = c.Name,
            //Command = UICommands.NavigateTo,
            CommandParameter = c.Id
        });
    }
    private void MainGrid_OnLoaded(object sender, RoutedEventArgs e)
    {
        State.Instance.Settings
            .WhenValueChanged(x => x.AppTheme)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Select(_ =>
            {
                var actualTheme = (MainWindow.Instance.Content as FrameworkElement)!.ActualTheme;
                UpdateColors(actualTheme, Player?.CurrentTrackColors);
                return default(Unit);
            })
            .Subscribe();
    }

    public void UpdateColors(ElementTheme theme, SpotifyColors? hexCode)
    {
        var clr = theme switch
        {
            ElementTheme.Dark => hexCode?.Dark?.ToColor() ?? Colors.Black,
            ElementTheme.Light => hexCode?.Light?.ToColor() ?? Colors.White,
            ElementTheme.Default => hexCode?.Light?.ToColor() ?? Colors.White,
            _ => throw new ArgumentOutOfRangeException(nameof(theme), theme, null)
        };
        //set alpha to 10%
        clr.A = (byte)(byte.MaxValue * .2);
        var brush = new SolidColorBrush(clr);
        ColorBackground.Background = brush;
    }

    public Brush IsOurDeviceForeground(SpotifyRemoteDeviceInfo spotifyRemoteDeviceInfo)
    {
        var isOurDevice = spotifyRemoteDeviceInfo.DeviceId == State.Instance.Client.DeviceId;
        return isOurDevice
            ? (Brush)Application.Current.Resources["ApplicationForegroundThemeBrush"]
            : (Brush)(Application.Current.Resources["SystemControlForegroundAccentBrush"]);
    }
}