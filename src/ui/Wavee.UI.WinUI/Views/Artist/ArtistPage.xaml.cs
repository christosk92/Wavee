using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Wavee.UI.Features.Artist.ViewModels;
using Wavee.UI.Features.Navigation.ViewModels;
using Wavee.UI.WinUI.Contracts;
using AsyncLock = NeoSmart.AsyncLock.AsyncLock;

namespace Wavee.UI.WinUI.Views.Artist;

public sealed partial class ArtistPage : Page, INavigeablePage<ArtistViewModel>
{
    private ArtistOverviewPage? _overviewPage;
    private readonly AsyncLock _asyncLock = new();
    private ArtistRelatedContentPage? _relatedContentPage;
    private ArtistAboutPage? _aboutPage;

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
        }
    }

    public void UpdateBindings()
    {
        //this.Bindings.Update();
    }

    public ArtistViewModel ViewModel => DataContext is ArtistViewModel vm ? vm : null;

    private async void Scroller_OnViewChanged(ScrollView sender, object args)
    {
        using (await _asyncLock.LockAsync())
        {
            var vm = ViewModel;
            if (vm is null)
            {
                return;
            }

            //If we changed the view while waiting for the lock, we should not do anything

            var hideBackgroundHeight = HideBackground.ActualHeight;
            var progress = Math.Clamp(sender.VerticalOffset / hideBackgroundHeight, 0, 1);
            HideBackground.Opacity = progress;

            // if (ViewModel.SelectedItem is ArtistOverviewViewModel ov)
            // {
            //     ov.ScrollPosition = sender.VerticalOffset;
            //     //Check if we are at the bottom of the scrollviewer with a 100px margin
            //     if (sender.VerticalOffset >= sender.ScrollableHeight - 200)
            //     {
            //         double epsilon = 0.0001;
            //         if (Math.Abs(beforeLock - afterLock) > epsilon)
            //         {
            //             Debug.WriteLine("View changed while waiting for lock");
            //             return;
            //         }
            //         Debug.WriteLine("Fetching next page");
            //
            //         await ov.FetchNextDiscography();
            //     }
            //
            //     await Task.Delay(100);
            // }
        }
    }
    private void ArtistPage_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (MainContent.Content is ArtistOverviewPage overviewPage)
        {
            var topTracksGridSize = overviewPage.SecondTopGridColumn.ActualWidth;
            var wdth = topTracksGridSize / 2;
            if (overviewPage.TopTracksGrid.ItemsPanelRoot is ItemsWrapGrid wrapGrid)
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
    public object ToView(NavigationItemViewModel navigationItemViewModel)
    {
        var pg = navigationItemViewModel switch
        {
            ArtistOverviewViewModel v => (_overviewPage ??= new ArtistOverviewPage(v)) as UserControl,
            ArtistRelatedContentViewModel r => _relatedContentPage ??= new ArtistRelatedContentPage(r),
            ArtistAboutViewModel a => _aboutPage ??= new ArtistAboutPage(a),
            _ => throw new ArgumentOutOfRangeException(nameof(navigationItemViewModel))
        };
        var dispatcher = this.DispatcherQueue;
        Task.Run(async () =>
        {
            await Task.Delay(10);
            dispatcher.TryEnqueue(() =>
            {
                switch (navigationItemViewModel)
                {
                    case ArtistOverviewViewModel v:
                        {
                            Scroller.ScrollTo(0, v.ScrollPosition, new ScrollingScrollOptions(ScrollingAnimationMode.Disabled));
                            break;
                        }
                }
            });
        });

        return pg;
    }
}
