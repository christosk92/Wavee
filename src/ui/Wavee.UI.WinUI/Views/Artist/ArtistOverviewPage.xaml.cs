using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NeoSmart.AsyncLock;
using Wavee.UI.Domain.Album;
using Wavee.UI.Features.Artist.ViewModels;

namespace Wavee.UI.WinUI.Views.Artist;

public sealed partial class ArtistOverviewPage : UserControl
{
    public ArtistOverviewPage(ArtistOverviewViewModel artistOverviewViewModel)
    {
        this.InitializeComponent();
        ViewModel = artistOverviewViewModel;
    }
    public ArtistOverviewViewModel ViewModel { get; }
    public Visibility NullIsCollapsed(SimpleAlbumEntity? simpleAlbumEntity)
    {
        return simpleAlbumEntity is null ? Visibility.Collapsed : Visibility.Visible;
    }
    // private async void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
    // {
    //    // await ViewModel.FetchNextDiscography(true);
    // }
}