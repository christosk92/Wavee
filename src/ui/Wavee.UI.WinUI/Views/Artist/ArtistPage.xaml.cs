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

public sealed partial class ArtistPage : Page,
    INavigeablePage<ArtistViewModel>,
    ISidebarListener
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
            this.SidebarOpened(_lastLeft, _lastRight);
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

    private bool _lastLeft;
    private bool _lastRight;
    public void SidebarOpened(bool left, bool right)
    {
        _lastLeft = left;
        _lastRight = right;
        ChangeSharedMargins(left, right);
    }


    void ChangeSharedMargins(bool leftOpen, bool rightOpen)
    {
        if (ViewModel is null)
        {
            return;
        }

        if (leftOpen || rightOpen)
        {
            HideBackground.Height = 120;
            ViewModel.ChildrenThickness = new SharedThickness(0, 14, 0, 0);
            RootGrid.Margin = new Thickness(0, -48, 0, 0);
            HideBackground.Margin =  new Thickness(4, 4, 22, 0);
        }
        else
        {
            HideBackground.Height = 230;
            ViewModel.ChildrenThickness = new SharedThickness(0);
            HideBackground.Margin =  new Thickness(0, 0, 0, 0);
            RootGrid.Margin = new Thickness(0, -148, 0, 0);
        }
    }
}
