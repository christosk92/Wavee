using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Threading;
using System.Threading.Tasks;
using Wavee.Core.Ids;
using Wavee.UI.Core;
using Wavee.UI.Core.Contracts.Artist;
using CommunityToolkit.Labs.WinUI;
using Microsoft.UI.Xaml.Media.Imaging;
using Wavee.UI.Navigation;
using Wavee.UI.ViewModel.Artist;
using Wavee.UI.WinUI.Navigation;
using Wavee.UI.WinUI.Views.Artist.Concert;
using Wavee.UI.WinUI.Views.Artist.Overview;
using System.Diagnostics;
using Microsoft.UI.Xaml.Media.Animation;

namespace Wavee.UI.WinUI.Views.Artist
{
    public sealed partial class ArtistPage : UserControl, INavigable, ICacheablePage
    {
        public ArtistPage()
        {
            this.InitializeComponent();
            ViewModel = new ArtistViewModel();
        }

        public SpotifyArtistView Artist { get; set; }
        public ArtistViewModel ViewModel { get; }
        private TaskCompletionSource<SpotifyArtistView> _artistFetched = new TaskCompletionSource<SpotifyArtistView>(TaskCreationOptions.RunContinuationsAsynchronously);
        private AudioId _id;
        public async void NavigatedTo(object parameter)
        {
            if (parameter is AudioId id)
            {
                ViewModel.Create(_id);
                _id = id;
                Artist = await Task.Run(() =>
                    Global.AppState.Artist.GetArtistViewAsync(id, CancellationToken.None));
                _artistFetched.TrySetResult(Artist);
                ArtistNameBlock.Text = Artist.Name;
                HeaderImage.Source = Artist.HeaderImage;
                var r = Artist.MonthlyListeners.ToString("N0");
                MonthlyListenersBlock.Text = $"{r} monthly listeners";
                MetadataPnale.Visibility = Visibility.Visible;
                ShowPanelAnim.Start();
                if (!string.IsNullOrEmpty(Artist.ProfilePicture))
                {
                    SecondPersonPicture.ProfilePicture = new BitmapImage(new Uri(Artist.ProfilePicture));
                }
                else
                {
                    SecondPersonPicture.DisplayName = Artist.Name;
                }

                if (string.IsNullOrEmpty(Artist.HeaderImage))
                {
                    //show picture
                    HeaderImage.Visibility = Visibility.Collapsed;
                    AlternativeArtistImage.Visibility = Visibility.Visible;
                    if (!string.IsNullOrEmpty(Artist.ProfilePicture))
                    {
                        AlternativeArtistImage.ProfilePicture = SecondPersonPicture.ProfilePicture;
                    }
                    else
                    {
                        AlternativeArtistImage.DisplayName = SecondPersonPicture.DisplayName;
                    }
                }
                ArtistPage_OnSizeChanged(this, null);
            }
        }
        public void RemovedFromCache()
        {
            Artist = null;
            _artistFetched = null;
            ViewModel?.Destroy();
            _overview?.ClearItems();
            _overview = null;
            _concerts = null;
            _about?.Clear();
            _about = null;

            Task.Run(() =>
            {
                Process prs = Process.GetCurrentProcess();
                try
                {
                    prs.MinWorkingSet = (IntPtr)(300000);
                }
                catch (Exception exception)
                {
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
            });
        }
        public void NavigatedFrom()
        {
            ViewModel?.Destroy();
        }
        private ArtistOverviewView? _overview;
        private ArtistConcertsView? _concerts;
        private ArtistAboutView? _about;
        private async void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var artist = await _artistFetched.Task;
            var selectedItems = e.AddedItems;
            if (selectedItems.Count > 0)
            {
                var item = (SegmentedItem)selectedItems[0];
                var content = item.Tag switch
                {
                    "overview" => _overview ??= new ArtistOverviewView(artist.Discography, artist.TopTracks),
                    "concerts" => _concerts ??= new ArtistConcertsView(),
                    "about" => (_about ??= new ArtistAboutView(artist.Id.ToBase62())) as UIElement,
                    _ => throw new ArgumentOutOfRangeException()
                };
                MainContent.Content = content;
            }
        }
        private bool _wasTransformed;


        private void ScrollViewer_OnViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var frac = ((ScrollViewer)sender).VerticalOffset / ImageT.Height;
            var progress = Math.Clamp(frac, 0, 1);
            HeaderImage.BlurValue = progress * 20;

            var exponential = Math.Pow(progress, 2);
            var opacity = 1 - exponential;
            HeaderImage.Opacity = opacity;

            //at around 75%, we should start transforming the header into a floating one
            const double threshold = 0.75;
            if (progress > 0.75 && !_wasTransformed)
            {
                _wasTransformed = true;
                BaseTrans.Source = MetadataPnale;
                BaseTrans.Target = SecondMetadataPanel;
                _ = BaseTrans.StartAsync();
            }
            else if (progress <= .75 && _wasTransformed)
            {
                _wasTransformed = false;
                BaseTrans.Source = SecondMetadataPanel;
                BaseTrans.Target = MetadataPnale;
                _ = BaseTrans.StartAsync();
            }
        }
        private void ArtistPage_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var newSize = (sender as FrameworkElement)?.ActualHeight ?? 0;
            //ratio is around 1:1, so 1/2
            if (!string.IsNullOrEmpty(HeaderImage.Source))
            {
                var topHeight = newSize * 0.5;
                topHeight = Math.Min(topHeight, 550);
                ImageT.Height = topHeight;
            }
            else
            {
                //else its only 1/4th
                var topHeight = newSize * 0.25;
                topHeight = Math.Min(topHeight, 550);
                ImageT.Height = topHeight;
            }
        }
        private void ImageT_OnContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {

        }

        public object FollowingToContent(bool b)
        {
            var stckp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8
            };
            if (b)
            {
                stckp.Children.Add(new FontIcon
                {
                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
                    Glyph = "\uE8F8"
                });
                stckp.Children.Add(new TextBlock
                {
                    Text = "Unfollow"
                });
            }
            else
            {
                stckp.Children.Add(new FontIcon { FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"), Glyph = "\uE8FA" });
                stckp.Children.Add(new TextBlock { Text = "Follow" });
            }

            return stckp;
        }

        public bool ShouldKeepInCache(int currentDepth)
        {
            return currentDepth <= 3;
        }

        private void ArtistPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            // var anim = ConnectedAnimationService.GetForCurrentView().GetAnimation("ForwardConnectedAnimation");
            // if (anim != null)
            // {
            //     anim.TryStart(ArtistNameBlock);
            // }
        }
    }
}
