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
            overviewPage.RootSizeChanged();
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

    public Thickness RootGridMargin(
        bool? leftSidebarIsOpen,
        double leftSidebarWidth,
        bool rightSidebarIsOpen,
        double rightSidebarWidth)
    {
        var baseThickness = new Thickness(0, -135, 0, 0);

        if (leftSidebarIsOpen is true)
        {
            baseThickness = new Thickness(
                left: -leftSidebarWidth + baseThickness.Top,
                top: baseThickness.Top,
                right: baseThickness.Right - 12,
                bottom: baseThickness.Bottom
            );
            // return new Thickness(-sidebarwidth, -135, 0, 0);
        }
        else
        {
            baseThickness = baseThickness;
        }

        if (rightSidebarIsOpen is true)
        {
            baseThickness = new Thickness(
                left: baseThickness.Left,
                top: baseThickness.Top,
                right: -rightSidebarWidth + baseThickness.Right,
                bottom: baseThickness.Bottom
            );
            // return new Thickness(0, -135, -sidebarwidth, 0);
        }
        else
        {
            baseThickness = baseThickness;
            // return new Thickness(0, -135, 0, 0);
        }

        return baseThickness;
    }

    public Thickness RestGridMargin(
        bool? leftSidebarIsOpen,
        double leftSidebarWidth,
        bool rightSidebarIsOpen,
        double rightSidebarWidth)
    {
        var baseThickness = new Thickness(24, 160, 24, 0);

        if (leftSidebarIsOpen is true)
        {
            baseThickness = new Thickness(
                left: leftSidebarWidth + baseThickness.Top,
                top: baseThickness.Top,
                right: baseThickness.Right,
                bottom: baseThickness.Bottom
            );

            //return new Thickness(d + 24, 160, 24, 0);
        }
        else
        {
            baseThickness = baseThickness;
            // return new Thickness(0, 0, 0, 0);
        }

        if (rightSidebarIsOpen is true)
        {
            baseThickness = new Thickness(
                left: baseThickness.Left,
                top: baseThickness.Top,
                right: rightSidebarWidth + baseThickness.Right,
                bottom: baseThickness.Bottom
            );
            //return new Thickness(0, 0, d + 24, 0);
        }
        else
        {
            baseThickness = baseThickness;
            //return new Thickness(0, 0, 0, 0);
        }

        return baseThickness;
    }

    public Thickness HideBackgroundMargin(bool? b, double d, bool b1, double d1)
    {
        //0,0,-12,0
        var baseThickenss = new Thickness(0, 0, -12, 0);

        if (b is true)
        {
            baseThickenss = new Thickness(
                left: -d + baseThickenss.Top - 24,
                top: baseThickenss.Top,
                right: baseThickenss.Right,
                bottom: baseThickenss.Bottom
            );
            //return new Thickness(-d, 0, -12, 0);
        }
        else
        {
            baseThickenss = baseThickenss;
            //return new Thickness(0, 0, -12, 0);
        }

        if (b1 is true)
        {
            baseThickenss = new Thickness(
                left: baseThickenss.Left,
                top: baseThickenss.Top,
                right: -d1 + baseThickenss.Right,
                bottom: baseThickenss.Bottom
            );
            //return new Thickness(0, 0, -d1, 0);
        }
        else
        {
            baseThickenss = baseThickenss;
            //return new Thickness(0, 0, -12, 0);
        }

        return baseThickenss;
    }

    public Thickness RootGridMargin_2(bool? b, bool b1)
    {
        //0,-48,0,0
        if (b is true || b1 is true)
        {
            return new Thickness(0, -48, 0, 0);
        }

        return new Thickness(0, -48, 0, 0);
    }
}
