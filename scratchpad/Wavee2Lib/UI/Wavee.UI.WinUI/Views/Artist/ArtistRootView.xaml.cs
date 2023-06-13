using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using LanguageExt;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Wavee.Core.Ids;
using Wavee.UI.ViewModels.Artist;
using Wavee.UI.WinUI.Navigation;
using ReactiveUI;
using Eum.Spotify.context;
using LanguageExt.UnsafeValueAccess;
using System.Windows.Input;
using Wavee.UI.ViewModels.Playback;
using DynamicData;
using CommunityToolkit.Labs.WinUI;
using LanguageExt.Pretty;
using Wavee.UI.WinUI.Views.Artist.About;
using Wavee.UI.Client;
using Wavee.UI.Client.Artist;
using Wavee.UI.WinUI.Views.Artist.Overview;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Artist
{
    public sealed partial class ArtistRootView : UserControl, ICacheablePage, INavigateablePage
    {
        private readonly TaskCompletionSource<SpotifyArtistView> _artistFetched = new TaskCompletionSource<SpotifyArtistView>();

        public ArtistRootView()
        {
            this.InitializeComponent();
            ViewModel = new ArtistViewModel();
        }

        public ArtistViewModel ViewModel { get; }
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

        public bool ShouldKeepInCache(int currentDepth)
        {
            return currentDepth <= 1;
        }

        public void RemovedFromCache()
        {
            _overview?.ClearItems();
            _overview = null;
            _concerts = null;
            _about?.Clear();
            _about = null;
            ViewModel.Destroy();
        }


        public async void NavigatedTo(object parameter)
        {
            if (parameter is not AudioId artistId)
            {
                return;
            }

            ViewModel.Create(artistId);

            var artist = await SpotifyView.FetchArtist(artistId);
            _artistFetched.TrySetResult(artist);
            ArtistNameBlock.Text = artist.Name;
            HeaderImage.Source = artist.HeaderImage;
            var r = artist.MonthlyListeners.ToString("N0");
            MonthlyListenersBlock.Text = $"{r} monthly listeners";
            MetadataPnale.Visibility = Visibility.Visible;
            ShowPanelAnim.Start();
            if (!string.IsNullOrEmpty(artist.ProfilePicture))
            {
                SecondPersonPicture.ProfilePicture = new BitmapImage(new Uri(artist.ProfilePicture));
            }
            else
            {
                SecondPersonPicture.DisplayName = artist.Name;
            }

            if (string.IsNullOrEmpty(artist.HeaderImage))
            {
                //show picture
                HeaderImage.Visibility = Visibility.Collapsed;
                AlternativeArtistImage.Visibility = Visibility.Visible;
                if (!string.IsNullOrEmpty(artist.ProfilePicture))
                {
                    AlternativeArtistImage.ProfilePicture = SecondPersonPicture.ProfilePicture;
                }
                else
                {
                    AlternativeArtistImage.DisplayName = SecondPersonPicture.DisplayName;
                }
            }

            //ArtistPage_OnSizeChanged
            this.ArtistPage_OnSizeChanged(this, null);
        }

        


        private bool _wasTransformed;

        private async void ScrollViewer_OnViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
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
                await BaseTrans.StartAsync();
            }
            else if (progress <= .75 && _wasTransformed)
            {
                _wasTransformed = false;
                BaseTrans.Source = SecondMetadataPanel;
                BaseTrans.Target = MetadataPnale;
                await BaseTrans.StartAsync();
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
    }
}