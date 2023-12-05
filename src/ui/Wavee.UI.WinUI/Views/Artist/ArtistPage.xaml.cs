using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Nito.AsyncEx;
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
    private static AsyncLock _asyncLock = new();
    private async void Scroller_OnViewChanged(ScrollView sender, object args)
    {
        using (await _asyncLock.LockAsync())
        {
            var vm = ViewModel;
            if (vm is null)
            {
                return;
            }

            var hideBackgroundHeight = HideBackground.ActualHeight;
            var progress = Math.Clamp(sender.VerticalOffset / hideBackgroundHeight, 0, 1);
            HideBackground.Opacity = progress;

            //Check if we are at the bottom of the scrollviewer with a 100px margin
            if (sender.VerticalOffset >= sender.ScrollableHeight - 100)
            {
                await vm.FetchNextDiscography(false);
            }
        }
    }

    private async void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.FetchNextDiscography(true);
    }
}
