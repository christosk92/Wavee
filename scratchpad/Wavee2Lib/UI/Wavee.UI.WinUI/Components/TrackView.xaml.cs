using CommunityToolkit.WinUI.UI;
using LanguageExt;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using NAudio.Wave;
using System;
using System.Linq;
using System.Windows.Input;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Wavee.Core.Ids;
using Wavee.Spotify.Infrastructure.Mercury.Models;
using Wavee.UI.ViewModels.Library;
using Wavee.UI.ViewModels.Playback;
using Wavee.UI.WinUI.Views.Artist;

namespace Wavee.UI.WinUI.Components
{
    public sealed partial class TrackView : UserControl
    {
        public static readonly DependencyProperty ViewProperty = DependencyProperty.Register(nameof(View),
            typeof(object), typeof(TrackView), new PropertyMetadata(default(object)));

        public static readonly DependencyProperty IndexProperty = DependencyProperty.Register(nameof(Index),
            typeof(int), typeof(TrackView), new PropertyMetadata(default(int)));

        public static readonly DependencyProperty ShowImageProperty = DependencyProperty.Register(nameof(ShowImage),
            typeof(bool), typeof(TrackView), new PropertyMetadata(default(bool), ImagePropertiesChanged));

        private static void ImagePropertiesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var x = (TrackView)d;
            x.HandleImagePropertiesChanged();
        }

        public static readonly DependencyProperty ImageUrlProperty =
            DependencyProperty.Register(nameof(ImageUrl),
                typeof(string), typeof(TrackView),
                new PropertyMetadata(default(string?)));

        public static readonly DependencyProperty AlternatingRowColorProperty =
            DependencyProperty.Register(nameof(AlternatingRowColor), typeof(bool), typeof(TrackView),
                new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty PlaybackStateProperty =
            DependencyProperty.Register(nameof(PlaybackState), typeof(TrackPlaybackState), typeof(TrackView),
                new PropertyMetadata(default(TrackPlaybackState), PlaybackStateChanged));

        public static readonly DependencyProperty IdProperty = DependencyProperty.Register(nameof(Id), typeof(AudioId),
            typeof(TrackView), new PropertyMetadata(default(AudioId), IdChanged));

        public static readonly DependencyProperty PlayCommandProperty = DependencyProperty.Register(nameof(PlayCommand),
            typeof(ICommand), typeof(TrackView), new PropertyMetadata(default(ICommand)));

        public TrackView()
        {
            this.InitializeComponent();
        }

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
        private void HandleImagePropertiesChanged()
        {
            //ImageBorder
            //if showimage = false, unload ImageBorder and set MainContent relativepanel RightOf = savedbutton
            if (ShowImage)
            {
                var buttonsPanel = this.FindName("ImageBorder") as UIElement;
                if (buttonsPanel != null)
                {
                    buttonsPanel.Visibility = Visibility.Visible;
                }

                if (!string.IsNullOrEmpty(ImageUrl))
                {
                    var bitmapImage = new BitmapImage();
                    AlbumImage.Source = bitmapImage;
                    bitmapImage.DecodePixelHeight = 24;
                    bitmapImage.DecodePixelWidth = 24;
                    bitmapImage.UriSource = new System.Uri(ImageUrl, UriKind.RelativeOrAbsolute);
                }
            }
            else
            {
                //   var buttonsPanel = this.FindName("ButtonsPanel") as UIElement;
                if (ImageBorder != null)
                {
                    ImageBorder.Visibility = Visibility.Collapsed;
                    AlbumImage.Source = null;
                    Microsoft.UI.Xaml.Markup.XamlMarkupHelper.UnloadObject(ImageBorder);
                }
            }
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
            if (PlaybackViewModel.Instance.CurrentTrack?.Id.Equals(id) is true)
            {
                PlaybackState = PlaybackViewModel.Instance.Paused
                    ? TrackPlaybackState.Paused
                    : TrackPlaybackState.Playing;
            }
            else
            {
                PlaybackState = TrackPlaybackState.None;
            }

            if (LibrariesViewModel.Instance.InLibrary(id))
            {
                SavedButton.IsChecked = true;
            }
            else
            {
                SavedButton.IsChecked = false;
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
            PlaybackViewModel.Instance.PauseChanged += PlaybackOnPauseChanged;
            PlaybackViewModel.Instance.CurrentTrackChanged += PlaybackOnCurrentTrackChanged;
        }

        private void UnregisterPlaybackEvents()
        {
            PlaybackViewModel.Instance.PauseChanged -= PlaybackOnPauseChanged;
            PlaybackViewModel.Instance.CurrentTrackChanged -= PlaybackOnCurrentTrackChanged;
        }

        private void PlaybackOnCurrentTrackChanged(object sender, TrackOrEpisode e)
        {
            if (e is null)
            {
                PlaybackState = TrackPlaybackState.None;
                return;
            }

            if (e.Id.Equals(Id))
            {
                if (PlaybackViewModel.Instance.Paused)
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
            if (PlaybackViewModel.Instance.CurrentTrack is null)
            {
                PlaybackState = TrackPlaybackState.None;
                return;
            }

            if (PlaybackViewModel.Instance.CurrentTrack.Id.Equals(Id))
            {
                if (PlaybackViewModel.Instance.Paused)
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
            return (!string.IsNullOrEmpty(img) ? ifTrue : !ifTrue) && ShowImage;
        }

        public Style GetStyleFor(int i)
        {
            return
                !AlternatingRowColor || (i % 2 == 0)
                    ? (Style)Application.Current.Resources["EvenBorderStyleGrid"]
                    : (Style)Application.Current.Resources["OddBorderStyleGrid"];
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
            PlaybackViewModel.Instance.ResumePauseCommand.Execute(null);
        }

        private void TrackView_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (e.PointerDeviceType is PointerDeviceType.Touch or PointerDeviceType.Pen)
            {
                PlayCommand.Execute(Id);
            }
        }

        private void SavedButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            // var isChecked = SavedButton.IsChecked ?? false;
            // if (isChecked)
            // {
            //     ShellViewModel<WaveeUIRuntime>.Instance.Library.SaveCommand.Execute(new ModifyLibraryCommand(
            //         Ids: Seq1(Id),
            //         Add: true
            //     ));
            // }
            // else
            // {
            //     ShellViewModel<WaveeUIRuntime>.Instance.Library.SaveCommand.Execute(new ModifyLibraryCommand(
            //         Ids: Seq1(Id),
            //         Add: false
            //     ));
            // }
        }

        private void TrackView_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            PlayCommand?.Execute(Id);
        }

        private void TrackView_OnContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            // //check if multiple items are selected.
            // //ascend the tree until we find the listview or ItemsView.
            // //
            // // var ids = FindListView(sender)
            // //     .Bind(x => FindSelectedItems(x))
            // //     .Match(
            // //         Some: x => x,
            // //         None: () => FindItemsView(sender)
            // //             .Bind(y => FindSelectedItems(y))
            // //     );
            // var ids = FindListView(sender)
            //     .BiBind(
            //         Some: r => FindSelectedItems(r),
            //         None: () => FindItemsView(sender)
            //             .Map(FindSelectedItems)
            //     ).IfNone(Seq1(Id));
            // if (ids.Length == 0)
            //     ids = Seq1(Id);
            //
            // Point point = new Point(0, 0);
            // var properFlyout = ids.ConstructFlyout();
            // if (args.TryGetPosition(sender, out point))
            // {
            //     properFlyout.ShowAt(sender, point);
            // }
            // else
            // {
            //     properFlyout.ShowAt((FrameworkElement)sender);
            // }
        }

        private static Option<ListView> FindListView(UIElement sender)
        {
            var exists = sender.FindAscendant<ListView>();
            if (exists is not null) return exists;
            return Option<ListView>.None;
        }

        private static Seq<AudioId> FindSelectedItems(ListView listView)
        {
            var selectedItems = listView.SelectedItems;
            if (selectedItems.Count == 0)
            {
                return LanguageExt.Seq<AudioId>.Empty;
            }

            var ids = selectedItems.Select(static c => FindIdProperty(c)).ToSeq();

            return ids;
        }

        private static Option<ItemsRepeater> FindItemsView(UIElement sender)
        {
            var exists = sender.FindAscendant<ItemsRepeater>();
            if (exists is not null) return exists;
            return Option<ItemsRepeater>.None;
        }

        private static Seq<AudioId> FindSelectedItems(ItemsRepeater itemsView)
        {
            return LanguageExt.Seq<AudioId>.Empty;
            // var selectedItems = itemsView.SelectionModel.SelectedItems;
            // if (selectedItems.Count == 0)
            // {
            //     return LanguageExt.Seq<AudioId>.Empty;
            // }
            //
            // var ids = selectedItems.Select(static c => FindIdProperty(c)).ToSeq();
            //
            // return ids;
        }

        private static AudioId FindIdProperty(object o)
        {
            return o switch
            {
                //LibraryTrack lb => lb.Track.Id,
                ArtistDiscographyTrack dt => dt.Id,
                ArtistTopTrackView x => x.Id,
                _ => default
            };
        }

        public Uri GetImage(string s)
        {
            if (s is null)
            {
                return new Uri("ms-appx:///Assets/StoreLogo.png");
            }

            return new Uri(s);
        }
    }
}
public enum TrackPlaybackState
{
    None,
    Playing,
    Paused
}