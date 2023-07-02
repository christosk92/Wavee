using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.System;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Wavee.UI.Client.Playback;
using Wavee.UI.User;
using Wavee.UI.ViewModel.Playback;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;
using Wavee.Id;
using Wavee.UI.ViewModel.Shell;
using Wavee.UI.WinUI.Navigation;
using Wavee.UI.WinUI.View.Album;
using Wavee.UI.WinUI.View.Artist;
using DispatcherQueuePriority = Microsoft.UI.Dispatching.DispatcherQueuePriority;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.View.Shell
{
    public sealed partial class BottomPlayerControl : UserControl
    {
        private Guid _positionCallbackGuid;
        public static readonly DependencyProperty UserProperty = DependencyProperty.Register(nameof(User), typeof(UserViewModel), typeof(BottomPlayerControl), new PropertyMetadata(default(UserViewModel)));
        public static readonly DependencyProperty PlaybackProperty = DependencyProperty.Register(nameof(Playback), typeof(PlaybackViewModel), typeof(BottomPlayerControl), new PropertyMetadata(default(PlaybackViewModel), PlaybackViewModelChanged));
        private readonly DispatcherQueue _dispatcher;
        public static readonly DependencyProperty RightSidebarProperty = DependencyProperty.Register(nameof(RightSidebar), typeof(RightSidebarViewModel), typeof(BottomPlayerControl), new PropertyMetadata(default(RightSidebarViewModel)));

        public BottomPlayerControl()
        { 
            this.InitializeComponent();
            _dispatcher = this.DispatcherQueue;
            NavigateToCommand = new RelayCommand<ItemWithId>(id =>
            {
                switch (id.Type)
                {
                    case AudioItemType.Album:
                        NavigationService.Instance.Navigate(typeof(AlbumView), id.Id);
                        break;
                    case AudioItemType.Artist:
                        NavigationService.Instance.Navigate(typeof(ArtistView), id.Id);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });
        }

        private static void PlaybackViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var x = (BottomPlayerControl)d;

            if (e.OldValue is PlaybackViewModel oldPlaybackViewModel)
            {
                oldPlaybackViewModel.ClearPositionCallback(x._positionCallbackGuid);
            }

            if (e.NewValue is PlaybackViewModel newPlaybackViewModel)
            {
                x._positionCallbackGuid = newPlaybackViewModel.RegisterPositionCallback(500, x.PositionChanged);
            }
        }
        public UserViewModel User
        {
            get => (UserViewModel)GetValue(UserProperty);
            set => SetValue(UserProperty, value);
        }

        public PlaybackViewModel Playback
        {
            get => (PlaybackViewModel)GetValue(PlaybackProperty);
            set => SetValue(PlaybackProperty, value);
        }

        public Visibility TrueIsCollapsed(bool b)
        {
            return b ? Visibility.Collapsed : Visibility.Visible;
        }

        private void Image_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ExpandImageButton.Visibility = Visibility.Visible;
        }

        private void Image_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            ExpandImageButton.Visibility = Visibility.Collapsed;
        }

        private void ExpandImageButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            User.Settings.ImageExpanded = !User.Settings.ImageExpanded;
        }

        public IEnumerable<MetadataItem> MapToItems(ItemWithId[] itemWithIds)
        {
            return itemWithIds?.Select(x => new MetadataItem
            {
                Label = x.Title,
                Command = NavigateToCommand,
                CommandParameter = x
            }) ?? Enumerable.Empty<MetadataItem>();
        }

        public ICommand NavigateToCommand { get;  }

        public RightSidebarViewModel RightSidebar
        {
            get => (RightSidebarViewModel)GetValue(RightSidebarProperty);
            set => SetValue(RightSidebarProperty, value);
        }

        public double GetTimestamp(TimeSpan? timeSpan)
        {
           return timeSpan?.TotalMilliseconds ?? 0;
        }

        public string FormatTimestamp(TimeSpan? timeSpan)
        {
            if(timeSpan.HasValue)
                return timeSpan.Value.ToString(@"mm\:ss");
            return "--:--";
        }
        private void PositionChanged(int obj)
        {
            var position = TimeSpan.FromMilliseconds(obj);
            _dispatcher.TryEnqueue(DispatcherQueuePriority.High, () =>
            {
                PositionSlider.Value = position.TotalMilliseconds;
                PositionText.Text = FormatTimestamp(position);
            });
        }

        private void SongName_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var titleId = Playback.Title;
            switch (titleId.Type)
            {
                case AudioItemType.Album:
                    NavigationService.Instance.Navigate(typeof(AlbumView), titleId.Id);
                    break;
            }
        }

        private async void LyricsButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            //wait 50 ms for toggle to complete
            await Task.Delay(50);
            var isChecked = LyricsButton.IsChecked;
            if (isChecked.HasValue && isChecked.Value)
            {
                RightSidebar.ShowView(RightSidebarView.Lyrics);
            }
            else
            {
                RightSidebar.Hide();
            }
        }
    }
}
