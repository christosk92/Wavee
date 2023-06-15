using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using CommunityToolkit.WinUI.UI;
using CommunityToolkit.WinUI.UI.Controls;
using DynamicData.Binding;
using LanguageExt;
using Microsoft.UI;
using ReactiveUI;
using Wavee.Spotify.Infrastructure.Mercury.Models;
using Wavee.Spotify.Infrastructure.PrivateApi.Contracts.Response;
using Wavee.Spotify.Infrastructure.Remote.Contracts;
using Wavee.UI.Core;
using Wavee.UI.ViewModel.Library;
using Wavee.UI.ViewModel.Playback;
using Image = Spotify.Metadata.Image;
using static LanguageExt.Prelude;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Playback
{
    public sealed partial class BottomPlayerControl : UserControl
    {
        public static readonly DependencyProperty PlayerProperty = DependencyProperty.Register(nameof(Player),
            typeof(PlaybackViewModel), typeof(BottomPlayerControl),
            new PropertyMetadata(default(PlaybackViewModel), PropertyChangedCallback));

        public BottomPlayerControl()
        {
            this.InitializeComponent();
            TimeElapsedElement.Text = "--:--";
        }

        private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var x = (BottomPlayerControl)d;
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
                Command = UICommands.NavigateTo,
                CommandParameter = c.Id
            });
        }

        private void MainGrid_OnLoaded(object sender, RoutedEventArgs e)
        {
            Global.AppState.UserSettings
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
            var isOurDevice = spotifyRemoteDeviceInfo.DeviceId == Global.AppState.DeviceId;
            return isOurDevice
                ? (Brush)Application.Current.Resources["ApplicationForegroundThemeBrush"]
                : (Brush)(Application.Current.Resources["SystemControlForegroundAccentBrush"]);
        }

        private async void StarButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var id = Player.CurrentTrack?.Id;

            if (id.HasValue)
            {
                await Task.Delay(20);
                LibrariesViewModel.Instance.SaveItem(Seq1(id.Value));
            }
        }
    }
}