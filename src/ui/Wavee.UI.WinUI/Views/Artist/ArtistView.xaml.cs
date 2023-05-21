using CommunityToolkit.Labs.WinUI;
using LanguageExt;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.ViewModels;

namespace Wavee.UI.WinUI.Views.Artist;

public sealed partial class ArtistRootView : UserControl, INavigablePage
{
    public ArtistRootView()
    {
        ViewModel = new ArtistViewModel<WaveeUIRuntime>(App.Runtime);
        this.InitializeComponent();
    }

    public ArtistViewModel<WaveeUIRuntime> ViewModel { get; }
    private async void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await ViewModel.ArtistFetched.Task;
        var selectedItems = e.AddedItems;
        if (selectedItems.Count > 0)
        {
            var item = (SegmentedItem)selectedItems[0];
            var content = item.Tag switch
            {
                "overview" => _overview ??= new ArtistOverview(ref ViewModel._artist),
                "concerts" => _concerts ??= new ArtistConcerts(ref ViewModel._artist),
                "about" => (_about ??= new ArtistAbout(ViewModel._artist.Id)) as UIElement,
                _ => throw new ArgumentOutOfRangeException()
            };
            MainContent.Content = content;
        }
    }

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
        if (progress >= 0.75)
        {
            BaseTrans.Source = MetadataPnale;
            BaseTrans.Target = SecondMetadataPanel;
            await BaseTrans.StartAsync();
        }
        else
        {
            BaseTrans.Source = SecondMetadataPanel;
            BaseTrans.Target = MetadataPnale;
            await BaseTrans.StartAsync();
        }
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
        SecondPersonPicture.ProfilePicture = new BitmapImage(new Uri(ViewModel.Artist.ProfilePicture));
    }

    private void ArtistPage_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        //ratio is around 1:1, so 1/2
        var topHeight = e.NewSize.Height * 0.5;
        topHeight = Math.Min(topHeight, 550);
        ImageT.Height = topHeight;
    }

    private void ArtistRootView_OnUnloaded(object sender, RoutedEventArgs e)
    {
        this.Bindings.StopTracking();
        _overview?.Clear();
        _overview = null;
        _concerts?.Clear();
        _concerts = null;
        _about?.Clear();
        _about = null;
    }
}