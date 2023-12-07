using System;
using System.Threading.Tasks;
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
    private void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
    {
        RootSizeChanged();
    }

    public void RootSizeChanged()
    {
        var topTracksGridSize = SecondTopGridColumn.ActualWidth;
        var wdth = topTracksGridSize / 2;
        if (TopTracksGrid.ItemsPanelRoot is ItemsWrapGrid wrapGrid)
        {
            if (wdth > 350)
            {
                wrapGrid.Orientation = Orientation.Vertical;
                wrapGrid.MaximumRowsOrColumns = 5;
                wrapGrid.ItemWidth = this.ActualWidth / 2 - 24;
            }
            else
            {
                wrapGrid.Orientation = Orientation.Vertical;
                wrapGrid.MaximumRowsOrColumns = 5;
                wrapGrid.ItemWidth = this.ActualWidth - 48;
            }
        }
    }

    private void TopTrackGrid_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        RootSizeChanged();
    }
}