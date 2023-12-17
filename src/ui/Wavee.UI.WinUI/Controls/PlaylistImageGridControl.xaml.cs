using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using Wavee.UI.Features.Playlists.ViewModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Controls
{
    public sealed partial class PlaylistImageGridControl : UserControl
    {
        public static readonly DependencyProperty TracksProperty = DependencyProperty.Register(nameof(Tracks), typeof(ObservableCollection<LazyPlaylistTrackViewModel>), typeof(PlaylistImageGridControl), new PropertyMetadata(default(ObservableCollection<LazyPlaylistTrackViewModel>), PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var x = (PlaylistImageGridControl)d;
            x.OnChanged(e.OldValue, e.NewValue);
        }

        private readonly DispatcherQueue _dispatcherQueue;
        public PlaylistImageGridControl()
        {
            this.InitializeComponent();
            _dispatcherQueue = this.DispatcherQueue;
        }

        public ObservableCollection<LazyPlaylistTrackViewModel> Tracks
        {
            get => (ObservableCollection<LazyPlaylistTrackViewModel>)GetValue(TracksProperty);
            set => SetValue(TracksProperty, value);
        }

        private void OnChanged(object eOldValue, object eNewValue)
        {
            if (eOldValue is ObservableCollection<LazyPlaylistTrackViewModel> oldOne)
            {
                oldOne.CollectionChanged -= CollChanged;
            }

            if (eNewValue is ObservableCollection<LazyPlaylistTrackViewModel> newOne)
            {
                newOne.CollectionChanged += CollChanged;
            }
        }

        private void CollChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                ReComputeGrid();
            });
        }

        private void ReComputeGrid()
        {
            var hasAnyTrack = this.Tracks.Any(f => f.HasValue);
            if (!hasAnyTrack)
            {
                // Empty grid
                ImageGrid.Visibility = Visibility.Collapsed;
                NoImage.Visibility = Visibility.Visible;
            }
            else
            {
                var fourImages = Tracks
                    .Where(x => x.HasValue)
                    .DistinctBy(x => x.Track!.BiggestImageUrl)
                    .ToImmutableArray();

                if (fourImages.Length < 4)
                {
                    // Just one image
                    AlbumCoverControl.Visibility = Visibility.Visible;
                    FirstImage.Visibility = Visibility.Collapsed;
                    SecondImage.Visibility = Visibility.Collapsed;
                    ThirdImage.Visibility = Visibility.Collapsed;
                    FourthImage.Visibility = Visibility.Collapsed;

                    var firstImage = fourImages.First().Track!.BiggestImageUrl;
                    AlbumCoverControl.Source = firstImage;
                }
                else
                {
                    AlbumCoverControl.Visibility = Visibility.Collapsed;
                    FirstImage.Visibility = Visibility.Visible;
                    SecondImage.Visibility = Visibility.Visible;
                    ThirdImage.Visibility = Visibility.Visible;
                    FourthImage.Visibility = Visibility.Visible;

                    var firstImage = fourImages[0].Track!.BiggestImageUrl;
                    var secondImage = fourImages[1].Track!.BiggestImageUrl;
                    var thirdImage = fourImages[2].Track!.BiggestImageUrl;
                    var fourthImage = fourImages[3].Track!.BiggestImageUrl;

                    SetImage(FirstImage, firstImage);
                    SetImage(SecondImage, secondImage);
                    SetImage(ThirdImage, thirdImage);
                    SetImage(FourthImage, fourthImage);
                }
                ImageGrid.Visibility = Visibility.Visible;
                NoImage.Visibility = Visibility.Collapsed;
            }
        }

        private void SetImage(Image firstImage, string p)
        {
            var bmp = new BitmapImage();
            bmp.DecodePixelHeight = 300;
            bmp.DecodePixelWidth = 300;
            bmp.UriSource = new Uri(p);
            firstImage.Source = bmp;
        }
    }
}
