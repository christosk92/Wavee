using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Labs.WinUI;
using LanguageExt.Pretty;
using LanguageExt.UnsafeValueAccess;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Wavee.UI.ViewModel.Artist;
using Wavee.UI.ViewModel.Shell;
using Wavee.UI.WinUI.Navigation;
using Wavee.UI.WinUI.View.Artist.Views;

namespace Wavee.UI.WinUI.View.Artist;

public sealed partial class ArtistView : UserControl, INavigable, ICacheablePage
{
    private ArtistOverviewView? _overview;
    private ArtistConcertsView? _concerts;
    private ArtistAboutView? _about;
    private ArtistRelatedView? _related;
    private bool _wasTransformed;
    private readonly TaskCompletionSource _artistFetched;
    public ArtistView()
    {
        _artistFetched = new TaskCompletionSource();
        this.InitializeComponent();
        ViewModel = new ArtistViewModel(ShellViewModel.Instance.User);
    }
    public ArtistViewModel ViewModel { get; }


    public object FollowingToContent(bool b)
    {
        return new Grid();
    }

    private async void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await _artistFetched.Task;
        var selectedItems = e.AddedItems;
        if (selectedItems.Count > 0)
        {
            var item = (SegmentedItem)selectedItems[0];
            var content = item.Tag switch
            {
                "overview" => _overview ??= new ArtistOverviewView(ViewModel.Artist),
                "related" => _related ??= new ArtistRelatedView(ViewModel.Artist),
                "concerts" => _concerts ??= new ArtistConcertsView(ViewModel.Artist),
                "about" => (_about ??= new ArtistAboutView(ViewModel.Artist)) as UIElement,
                _ => throw new ArgumentOutOfRangeException()
            };
            MainContent.Content = content;
        }
    }

    private void ArtistPage_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var newSize = (sender as FrameworkElement)?.ActualHeight ?? 0;
        //ratio is around 1:1, so 1/2
        if (!string.IsNullOrEmpty(HeaderImage.Source))
        {
            var topHeight = newSize * 0.5;
            topHeight = Math.Min(topHeight, 650);
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

    private void ArtistPage_OnLoaded(object sender, RoutedEventArgs e)
    {

    }


    private void ScrollViewer_OnViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        var frac = (sender as ScrollViewer).VerticalOffset / ImageT.Height;
        var progress = Math.Clamp(frac, 0, 1);
        HeaderImage.BlurValue = progress * 5;

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

    public async void NavigatedTo(object parameter)
    {
        if (parameter is string id)
        {
            await ViewModel.Fetch(id, CancellationToken.None);
            ViewModel.CreateListener();
            _artistFetched.TrySetResult();
        }
        MetadataPnale.Visibility = Visibility.Visible;
        _ = ShowPanelAnim.StartAsync();
        if (!string.IsNullOrEmpty(ViewModel.Artist.AvatarImage.Url))
        {
            SecondPersonPicture.ProfilePicture = new BitmapImage(new Uri(ViewModel.Artist.AvatarImage.Url));
        }
        else
        {
            SecondPersonPicture.DisplayName = ViewModel.Artist.Name;
        }

        if (string.IsNullOrEmpty(ViewModel.Header))
        {
            //show picture
            HeaderImage.Visibility = Visibility.Collapsed;
            AlternativeArtistImage.Visibility = Visibility.Visible;
            if (!string.IsNullOrEmpty(ViewModel.Artist.AvatarImage.Url))
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

    public void NavigatedFrom(NavigationMode mode)
    {
        ViewModel.Destroy();
    }

    public bool ShouldKeepInCache(int currentDepth)
    {
        return currentDepth <= 2;
    }

    public void RemovedFromCache()
    {

    }
}