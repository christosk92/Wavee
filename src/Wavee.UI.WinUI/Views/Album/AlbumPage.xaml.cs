using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Wavee.UI.Core.Contracts.Album;
using Wavee.UI.Navigation;
using Wavee.UI.ViewModel.Album;
using CommunityToolkit.WinUI.UI.Controls;
using Wavee.UI.WinUI.Components;
using Windows.Foundation.Metadata;
using CommunityToolkit.Mvvm.Input;
using Eum.Spotify.context;
using LanguageExt;
using Wavee.Core.Ids;
using Wavee.UI.Core.Contracts.Artist;
using Wavee.UI.ViewModel.Playback;
using Wavee.UI.WinUI.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Album
{
    public sealed partial class AlbumPage : UserControl, INavigable, IPlayableView, ICacheablePage
    {
        public AlbumPage()
        {
            this.InitializeComponent();
            ViewModel = new AlbumViewModel();
            PlayTrackCommand = new AsyncRelayCommand<Option<TrackView>>(PlayTrack);
        }

        private Task PlayTrack(Option<TrackView> item)
        {
            if (item.IsNone)
            {
                //play from start
                PlaybackViewModel.Instance.PlayCommand
                    .Execute(new PlayContextStruct(
                        ContextId: ViewModel.AlbumId.ToString(),
                        ContextUrl: $"context://{ViewModel.AlbumId.ToString()}",
                        Index: 0,
                        TrackId: Option<AudioId>.None,
                        NextPages: Option<IEnumerable<ContextPage>>.None,
                        PageIndex: Option<int>.None,
                        Metadata: HashMap<string, string>.Empty
                    ));
                return Task.CompletedTask;
            }


            var track = item.IfNoneUnsafe(() => throw new Exception("track is none"));
            var trackIndex = ViewModel.Discs.SelectMany(x => x.Tracks).ToList()
                .FindIndex(x => x.Id == track.Id);
            PlaybackViewModel.Instance.PlayCommand
                .Execute(new PlayContextStruct(
                    ContextId: ViewModel.AlbumId.ToString(),
                    ContextUrl: $"context://{ViewModel.AlbumId.ToString()}",
                    Index: trackIndex,
                    TrackId: track.Id,
                    NextPages: Option<IEnumerable<ContextPage>>.None,
                    PageIndex: Option<int>.None,
                    Metadata: HashMap<string, string>.Empty
                ));
            return Task.CompletedTask;
        }

        public AlbumViewModel ViewModel { get; }

        public async void NavigatedTo(object parameter)
        {
            if (parameter is NavigationWithImage img)
            {
                var bmp = new BitmapImage();
                Src.Source = bmp;
                bmp.UriSource = new Uri(img.Image, UriKind.RelativeOrAbsolute);

                var anim = ConnectedAnimationService.GetForCurrentView().GetAnimation("ForwardConnectedAnimation");
                if (anim != null)
                {
                    anim.Configuration = new DirectConnectedAnimationConfiguration();
                    anim.TryStart(Src);
                }
                ViewModel.AlbumImage = "something";
                _navigatedToSomethingWithBack = true;
                await ViewModel.Create(img.Id);
            }
            else if (parameter is AudioId id)
            {
                // Source="{x:Bind ViewModel.AlbumImage, Mode=OneWay}" create a binding
                var binding = new Binding();
                binding.Source = ViewModel;
                binding.Path = new PropertyPath(nameof(ViewModel.AlbumImage));
                binding.Mode = BindingMode.OneWay;
                BindingOperations.SetBinding(Src, Image.SourceProperty, binding);
                _navigatedToSomethingWithBack = false;
                await ViewModel.Create(id);
            }
        }

        private bool _navigatedToSomethingWithBack;
        public void NavigatedFrom(NavigationMode mode)
        {
            if (mode is NavigationMode.Back && _navigatedToSomethingWithBack)
            {
                ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("BackConnectedAnimation", Src);
            }
        }

        private void TracksList_OnLoaded(object sender, RoutedEventArgs e)
        {
            var scroller = ((ListView)sender).FindDescendant<ScrollViewer>();

            scroller.Unloaded += ScrollerOnUnloaded;
            scroller.ViewChanged += Scroller_ViewChanged;
        }

        private void ScrollerOnUnloaded(object sender, RoutedEventArgs e)
        {
            var scroller = (ScrollViewer)sender;

            scroller.Unloaded -= ScrollerOnUnloaded;
            scroller.ViewChanged -= Scroller_ViewChanged;
        }
        private bool _wasTransformed;

        private void Scroller_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            //check if we scrolled past final header
            var frac = (sender as ScrollViewer).VerticalOffset / ((UIElement)TracksList.Header).ActualSize.Y;
            var progress = Math.Clamp(frac, 0, 1);
            // HeaderImage.BlurValue = progress * 20;

            var exponential = Math.Pow(progress, 2);
            var opacity = 1 - exponential;
            // HeaderImage.Opacity = opacity;

            //at around 75%, we should start transforming the header into a floating one
            if (progress > .75 && !_wasTransformed)
            {
                _wasTransformed = true;
                BaseTrans.Source = MetadataPanel;
                BaseTrans.Target = SecondMetadataPanel;
                _ = BaseTrans.StartAsync();
            }
            else if (progress <= .75 && _wasTransformed)
            {
                _wasTransformed = false;
                BaseTrans.Source = SecondMetadataPanel;
                BaseTrans.Target = MetadataPanel;
                _ = BaseTrans.StartAsync();
            }
        }

        public object GetAppropriateCollection(IReadOnlyList<SpotifyDiscView> spotifyDiscViews)
        {
            if (spotifyDiscViews is null) return null;
            if (spotifyDiscViews.Count == 1)
            {
                return new CollectionViewSource
                {
                    Source = spotifyDiscViews[0].Tracks
                }.View;
            }
            else
            {
                return new CollectionViewSource
                {
                    Source = spotifyDiscViews,
                    IsSourceGrouped = true,
                    ItemsPath = new PropertyPath(nameof(SpotifyDiscView.Tracks))
                }.View;
            }
        }

        private void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {

        }
        //
        // private async void Title_OnIsTextTrimmedChanged(TextBlock sender, IsTextTrimmedChangedEventArgs args)
        // {
        //     //if we are trimming, it means we are too long
        //     //resize font size until we are no longer trimming
        //     while (sender.IsTextTrimmed)
        //     {
        //         sender.FontSize -= 1;
        //         await Task.Delay(TimeSpan.FromMilliseconds(10));
        //     }
        // }
        public AsyncRelayCommand<Option<TrackView>> PlayTrackCommand { get; }

        private bool isAdjusting = false;
        private const double maxFontSize = 46; // set your max font size
        private const double minFontSize = 10; // set your min font size
        private async void Title_OnIsTextTrimmedChanged(TextBlock sender, IsTextTrimmedChangedEventArgs args)
        {

        }

        private void AlbumPage_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var titleIsTrimmed = Title.IsTextTrimmed;

        }

        private void TitleBorderSizeChanged(object sender, SizeChangedEventArgs e)
        {
            //check how much space we have
            var width = e.NewSize.Width;
            //var currentFontSize = AlbumType.FontSize;

            if (Title.ActualWidth == 0) return;
            //check if we can fit the text
            var textWidth = Title.ActualWidth;
            //calculate the appropriate new fontsize (not a ratio)
            var newFontSize = Math.Max(minFontSize, Math.Min(maxFontSize, Title.FontSize * width / textWidth));
            Title.FontSize = newFontSize;

            // if (textWidth > width)
            // {
            //     //we can't fit the text, so we need to shrink it
            //     //calculate the appropriate 
            //
            //     AlbumType.FontSize = newFontSize;
            // }
            // else
            // {
            //     //we can fit the text, so we need to grow it
            //
            //     AlbumType.FontSize = newFontSize;
            // }
        }

        private void Artists_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var clickedItem = e.ClickedItem as SpotifyAlbumArtistView;

            UICommands.NavigateTo.Execute(clickedItem.Id);
        }

        public bool ShouldKeepInCache(int currentDepth)
        {
            return currentDepth <= 2;
        }

        public void RemovedFromCache()
        {
        }

        private void ArtistItemTapped(object sender, TappedRoutedEventArgs e)
        {
            var tag = (sender as FrameworkElement).Tag;
            if (tag is AudioId id)
            {
                UICommands.NavigateTo.Execute(id);
            }
        }

        private void PlayFromStart(object sender, TappedRoutedEventArgs e)
        {
            PlayTrackCommand.Execute(Option<TrackView>.None);
        }
    }
}
