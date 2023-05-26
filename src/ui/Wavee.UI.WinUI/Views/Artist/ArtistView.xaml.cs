using CommunityToolkit.Labs.WinUI;
using LanguageExt;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.ViewModels;
using Microsoft.UI.Xaml.Hosting;
using CommunityToolkit.WinUI.UI.Animations.Expressions;
using System.Windows.Controls;
using Windows.Foundation;
using Windows.UI;
using Eum.Spotify.context;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Wavee.Core.Ids;
using Border = Microsoft.UI.Xaml.Controls.Border;
using Orientation = Microsoft.UI.Xaml.Controls.Orientation;
using ScrollViewer = Microsoft.UI.Xaml.Controls.ScrollViewer;
using SelectionChangedEventArgs = Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs;
using StackPanel = Microsoft.UI.Xaml.Controls.StackPanel;
using TextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using UserControl = Microsoft.UI.Xaml.Controls.UserControl;
using Wavee.UI.ViewModels.Artist;
using Wavee.UI.WinUI.Flyouts;

namespace Wavee.UI.WinUI.Views.Artist;

public sealed partial class ArtistRootView : UserControl, INavigablePage
{
    public ArtistRootView()
    {
        ViewModel = new ArtistViewModel<WaveeUIRuntime>(App.Runtime);
        this.InitializeComponent();
    }

    public ArtistViewModel<WaveeUIRuntime> ViewModel { get; }
    public void RemovedFromCache()
    {
        ViewModel.Clear();
        _overview?.Clear();
        _overview = null;
        _concerts?.Clear();
        _concerts = null;
        _about?.Clear();
        _about = null;
        GC.Collect();
    }

    private async void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await ViewModel.ArtistFetched.Task;
        var selectedItems = e.AddedItems;
        if (selectedItems.Count > 0)
        {
            var item = (SegmentedItem)selectedItems[0];
            var content = item.Tag switch
            {
                "overview" => _overview ??= new ArtistOverview(ViewModel.Artist),
                "concerts" => _concerts ??= new ArtistConcerts(ViewModel.Artist),
                "about" => (_about ??= new ArtistAbout(ViewModel.Artist.Id.ToBase62())) as UIElement,
                _ => throw new ArgumentOutOfRangeException()
            };
            MainContent.Content = content;
        }
    }

    private bool _wasTransformed;

    private async void ScrollViewer_OnViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        var frac = ((ScrollViewer)sender).VerticalOffset / ImageT.Height;
        var progress = Math.Clamp(frac, 0, 1);
        Img.BlurValue = progress * 20;

        var exponential = Math.Pow(progress, 2);
        var opacity = 1 - exponential;
        Img.Opacity = opacity;

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

    private void ScrollViewer_OnViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
    {
        //in case we overscroll up, we cant a bounce effect on the image
    }

    Option<INavigableViewModel> INavigablePage.ViewModel => ViewModel;

    public bool ShouldKeepInCache(int depth)
    {
        //only 1 down (navigating to album)
        return depth <= 1;
    }

    public string FormatListeners(ulong @ulong)
    {
        //we want the result like 1,212,212;
        var r = @ulong.ToString("N0");
        return $"{r} monthly listeners";
    }

    private ArtistOverview? _overview;
    private ArtistConcerts? _concerts;
    private ArtistAbout? _about;

    private async void ArtistRootView_OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.ArtistFetched.Task;
        this.Bindings.Update();
        MetadataPnale.Visibility = Visibility.Visible;
        ShowPanelAnim.Start();
        if (!string.IsNullOrEmpty(ViewModel.Artist.ProfilePicture))
        {
            SecondPersonPicture.ProfilePicture = new BitmapImage(new Uri(ViewModel.Artist.ProfilePicture));
        }
        else
        {
            SecondPersonPicture.DisplayName = ViewModel.Artist.Name;
        }

        if (string.IsNullOrEmpty(ViewModel.Artist.HeaderImage))
        {
            //show picture
            Img.Visibility = Visibility.Collapsed;
            AlternativeArtistImage.Visibility = Visibility.Visible;
            if (!string.IsNullOrEmpty(ViewModel.Artist.ProfilePicture))
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

    private void ArtistPage_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var newSize = (sender as FrameworkElement)?.ActualHeight ?? 0;
        //ratio is around 1:1, so 1/2
        if (!string.IsNullOrEmpty(ViewModel.Artist?.HeaderImage))
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

    private void ArtistRootView_OnUnloaded(object sender, RoutedEventArgs e)
    {
    }

    private async void PlayLargeButton_OnTapped(object sender, TappedRoutedEventArgs e)
    {
        var context = new PlayContextStruct(
            ContextId: ViewModel.Artist.Id,
            ContextUrl: $"context://{ViewModel.Artist.Id}",
            Index: 0,
            NextPages: Option<IEnumerable<ContextPage>>.None,
            PageIndex: Option<int>.None,
            TrackId: Option<AudioId>.None
        );
        await ShellViewModel<WaveeUIRuntime>.Instance.Playback.PlayContextAsync(context);
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

    // public Brush GetBrushForFollowing(bool b)
    // {
    //     //ApplicationForegroundThemeBrush
    //     return b ? (Brush)Application.Current.Resources["ApplicationForegroundThemeBrush"]
    //         : new SolidColorBrush(Colors.Transparent);
    // }

    private void FollowButton_OnTapped(object sender, TappedRoutedEventArgs e)
    {
        ViewModel.FollowCommand.Execute(new ModifyLibraryCommand(Seq1(ViewModel.Artist.Id), !ViewModel.IsFollowing));
    }

    private void ImageT_OnContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        Point point = new Point(0, 0);
        var properFlyout = ViewModel.Artist.Id.ConstructFlyout();
        if (args.TryGetPosition(sender, out point))
        {
            properFlyout.ShowAt(sender, point);
        }
        else
        {
            properFlyout.ShowAt((FrameworkElement)sender);
        }
    }
}