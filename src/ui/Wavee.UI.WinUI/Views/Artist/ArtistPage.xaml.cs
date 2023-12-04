using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Wavee.UI.Domain.Album;
using Wavee.UI.Features.Artist.ViewModels;
using Wavee.UI.Features.Library.ViewModels.Album;
using Wavee.UI.WinUI.Contracts;

namespace Wavee.UI.WinUI.Views.Artist;

public sealed partial class ArtistPage : Page, INavigeablePage<ArtistViewModel>
{
    public ArtistPage()
    {
        this.InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is ArtistViewModel vm)
        {
            DataContext = vm;
            await vm.Initialize();
            ArtistPage_OnSizeChanged(null, null);
        }
    }

    public void UpdateBindings()
    {
        //this.Bindings.Update();
    }

    public ArtistViewModel ViewModel => DataContext is ArtistViewModel vm ? vm : null;

    public Visibility NullIsCollapsed(SimpleAlbumEntity? simpleAlbumEntity)
    {
        return simpleAlbumEntity is null ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ArtistPage_OnSizeChanged(object sender, SizeChangedEventArgs e)
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
}
