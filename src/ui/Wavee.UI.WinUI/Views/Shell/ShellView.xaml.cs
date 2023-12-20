using System;
using System.Windows.Forms;
using ABI.Microsoft.UI.Xaml.Media.Animation;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Wavee.UI.Extensions;
using Wavee.UI.Features.Artist.ViewModels;
using Wavee.UI.Features.Library.ViewModels;
using Wavee.UI.Features.Library.ViewModels.Album;
using Wavee.UI.Features.Library.ViewModels.Artist;
using Wavee.UI.Features.Listen;
using Wavee.UI.Features.Navigation;
using Wavee.UI.Features.Navigation.ViewModels;
using Wavee.UI.Features.NowPlaying.ViewModels;
using Wavee.UI.Features.Playlists.ViewModel;
using Wavee.UI.Features.RightSidebar.ViewModels;
using Wavee.UI.Features.Search.ViewModels;
using Wavee.UI.Features.Shell.ViewModels;
using Wavee.UI.WinUI.Contracts;
using Wavee.UI.WinUI.Services;
using Wavee.UI.WinUI.Views.Artist;
using Wavee.UI.WinUI.Views.Search;
using NavigationTransitionInfo = Microsoft.UI.Xaml.Media.Animation.NavigationTransitionInfo;
using UserControl = Microsoft.UI.Xaml.Controls.UserControl;

namespace Wavee.UI.WinUI.Views.Shell;

public sealed partial class ShellView : UserControl
{
    public ShellView()
    {
        this.InitializeComponent();
    }

    public ShellViewModel ViewModel
    {
        get
        {
            try
            {
                return DataContext is ShellViewModel vm ? vm : null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }

    private void NavigationView_OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer?.Tag is object item)
        {
            //ince sidebarviewmodel is a viewmodelbase, we need to pass the type as the actual upper time
            //so we can navigate to the correct page
            switch (item)
            {
                case ListenViewModel h:
                    ViewModel.Navigation.Navigate(args.RecommendedNavigationTransitionInfo, h);
                    break;
                case LibrarySongsViewModel s:
                    {
                        ViewModel.Navigation.Navigate(args.RecommendedNavigationTransitionInfo, s);
                        break;
                    }
                case LibraryAlbumsViewModel s:
                    {
                        ViewModel.Navigation.Navigate(args.RecommendedNavigationTransitionInfo, s);
                        break;
                    }
                case LibraryArtistsViewModel s:
                    {
                        ViewModel.Navigation.Navigate(args.RecommendedNavigationTransitionInfo, s);
                        break;
                    }
                case LibraryPodcastsViewModel s:
                    {
                        ViewModel.Navigation.Navigate(args.RecommendedNavigationTransitionInfo, s);
                        break;
                    }
                case ArtistOverviewViewModel:
                case ArtistAboutViewModel:
                case ArtistRelatedContentViewModel:
                    {
                        if (ViewModel.SelectedItem is ArtistViewModel artistRoot)
                        {
                            artistRoot.SelectedItem = item as NavigationItemViewModel;
                        }

                        break;
                    }
                case LibrariesViewModel p:
                    {
                        var selected = p.SelectedItem;
                        switch (selected)
                        {
                            case LibrarySongsViewModel s:
                                {
                                    ViewModel.Navigation.Navigate(args.RecommendedNavigationTransitionInfo, s);
                                    break;
                                }
                            case LibraryAlbumsViewModel s:
                                {
                                    ViewModel.Navigation.Navigate(args.RecommendedNavigationTransitionInfo, s);
                                    break;
                                }
                            case LibraryArtistsViewModel s:
                                {
                                    ViewModel.Navigation.Navigate(args.RecommendedNavigationTransitionInfo, s);
                                    break;
                                }
                            case LibraryPodcastsViewModel s:
                                {
                                    ViewModel.Navigation.Navigate(args.RecommendedNavigationTransitionInfo, s);
                                    break;
                                }
                        }

                        break;
                    }
                case NowPlayingViewModel c:
                    ViewModel.Navigation.Navigate(args.RecommendedNavigationTransitionInfo, c);
                    break;
            }
        }
    }

    private bool _initialized = false;
    private ArtistPage? _currentPage;
    private async void ShellView_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (_initialized)
            return;
        if (ViewModel?.Navigation is WinUINavigationService nav)
        {
            nav.Initialize(NavigationFrame);
            _initialized = true;
        }

        if (ViewModel is not null)
        {
            var vm = ViewModel;
            var navService = new WinUINavigationService(Constants.ServiceProvider);
            vm.RightSidebar.Navigation = navService;
            navService.Initialize(RightSidebarNavigationFrame);
            _initialized = true;

            void NotifyPageOfSidebar(object? currentPageSourceOverride = null)
            {
                var leftOpen = ViewModel.Playlists.ShowSidebar;
                var rightOpen = ViewModel.RightSidebar.IsOpen;

                if ((currentPageSourceOverride ?? ViewModel.Navigation.CurrentPageSource) is ISidebarListener s)
                {
                    s.SidebarOpened(leftOpen ?? false, rightOpen);
                }

                void SetColumnAndColumnSpan(FrameworkElement f, int col, int span)
                {
                    Grid.SetColumn(f, col);
                    Grid.SetColumnSpan(f, span);
                }

                if (leftOpen is true && !rightOpen)
                {
                    // | ---
                    SetColumnAndColumnSpan(NavigationFrame, 1, 2);
                    SetColumnAndColumnSpan(SecondaryNavView, 1, 2);
                }
                else if (leftOpen is true && rightOpen is true)
                {
                    // | --- |
                    SetColumnAndColumnSpan(NavigationFrame, 1, 1);
                    SetColumnAndColumnSpan(SecondaryNavView, 1, 1);
                }
                else if (leftOpen is false && rightOpen)
                {
                    // --- |
                    SetColumnAndColumnSpan(NavigationFrame, 0, 2);
                    SetColumnAndColumnSpan(SecondaryNavView, 0, 2);
                }
                else if (leftOpen is false && !rightOpen)
                {
                    // -----
                    SetColumnAndColumnSpan(NavigationFrame, 0, 3);
                    SetColumnAndColumnSpan(SecondaryNavView, 0, 3);
                }
            }

            vm.Navigation.NavigatedTo += (sender, o) =>
            {
                // //this.Bindings.Update();
                // RightSidebarGrid.Margin = new Thickness(0, 8, 0, 0);
                // PlaylistsGrid.Margin = new Thickness(12, 12, 12, 100);
                // if (ViewModel.Navigation.CurrentPageSource is ArtistPage pg)
                // {
                //     _currentPage = pg;
                //     pg.Scroller.ViewChanged += ScrollerOnViewChanged;
                // }
                // else if (_currentPage is not null)
                // {
                //     _currentPage.Scroller.ViewChanged -= ScrollerOnViewChanged;
                //     _currentPage = null;
                // }
                NotifyPageOfSidebar();
            };

            vm.RightSidebar.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(RightSidebarViewModel.IsOpen))
                {
                    NotifyPageOfSidebar();
                    if (ViewModel.RightSidebar.IsOpen)
                    {
                       // RightGridColumn.Width = new GridLength(1, GridUnitType.Star);
                        //RightSidebarGrid.Width = ViewModel.RightSidebar.SidebarWidth;
                    }
                    else
                    {
                        //RightGridColumn.Width = new GridLength(0);
                    }
                }
                else if (e.PropertyName == nameof(RightSidebarViewModel.SidebarWidth))
                {
                    if (Math.Abs(ViewModel.RightSidebar.SidebarWidth - RightSidebarGrid.ActualWidth) > 0.01)
                    {
                        RightSidebarGrid.Width = ViewModel.Playlists.SidebarWidth;
                    }
                }
            };
            vm.Playlists.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == nameof(PlaylistsNavItem.ShowSidebar))
                {
                    NotifyPageOfSidebar();
                    if (ViewModel.Playlists.ShowSidebar is true)
                    {
                        //LeftGridColumn.Width = new GridLength(1, GridUnitType.Star);
                       // PlaylistsGrid.Width = ViewModel.Playlists.SidebarWidth;
                    }
                    else
                    {
                      //  LeftGridColumn.Width = new GridLength(0);
                    }
                }
                else if (e.PropertyName == nameof(PlaylistsNavItem.SidebarWidth))
                {
                    if (Math.Abs(ViewModel.Playlists.SidebarWidth - PlaylistsGrid.ActualWidth) > 0.01)
                    {
                        PlaylistsGrid.Width = ViewModel.Playlists.SidebarWidth;
                    }
                }
            };

            await vm.Playlists.Playlists.Initialize().ConfigureAwait(false);
        }

    }

    // private void ScrollerOnViewChanged(ScrollView sender, object args)
    // {
    //     if (_currentPage is not null)
    //     {
    //         var opacity = _currentPage.HideBackground.Opacity; // 0 -> 1
    //         var progress = 54 * opacity;
    //         var baseRightThickness = RightSidebarGrid.Margin;
    //         RightSidebarGrid.Margin = new Thickness(baseRightThickness.Left,
    //             progress + 8,
    //             baseRightThickness.Right,
    //             baseRightThickness.Bottom);
    //
    //         var baseLeftThickness = PlaylistsGrid.Margin;
    //         PlaylistsGrid.Margin = new Thickness(baseLeftThickness.Left,
    //             progress + 8,
    //             baseLeftThickness.Right,
    //             baseLeftThickness.Bottom);
    //     }
    // }

    public Visibility HasSubItemsThenVisible(NavigationItemViewModel[]? navigationItemViewModels)
    {
        return navigationItemViewModels?.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void AutoSuggestBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason is AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ViewModel.Search.Query = sender.Text;
            //if we are on the search page, we want to update the suggestions
            if (ViewModel.Navigation.CurrentPage != typeof(SearchPage))
            {
                await ViewModel.Search.SearchSuggestions();
            }
            else
            {
                ViewModel.Search.Suggestions.Clear();
                await ViewModel.Search.Search();
            }
        }
    }

    private void AutoSuggestBox_OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {

    }

    private async void AutoSuggestBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var query = args.ChosenSuggestion;
        if (query is SearchSuggestionQueryViewModel q)
        {
            ViewModel.Search.Query = q.Query;
            ViewModel.Navigation.Navigate<SearchViewModel>(null);
            await ViewModel.Search.Search();
        }
        else if (query is SearchSuggestionEntityViewModel entity)
        {
            Constants.NavigationCommand.Execute(entity.Id);
        }
    }
    public bool CompositeBool(bool x, bool y, bool xShouldBe, bool yShouldBe)
    {
        if (x == xShouldBe && y == yShouldBe)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool Negate(bool b)
    {
        return !b;
    }

    public int ToIndex(RightSidebarItemViewModel x)
    {
        return ViewModel.RightSidebar.Items.IndexOf(x);
    }

    public void SetItem(int o)
    {
        ViewModel.RightSidebar.SelectedItem = ViewModel.RightSidebar.Items[o];
    }

    private void SidebarItemSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var removed = e.RemovedItems;
        var added = e.AddedItems;
        NavigationTransitionInfo? info = null;
        if (added.Count is not 0)
        {
            var addedOne = added[0];
            var addedOneIndex = (sender as Segmented).Items.IndexOf(addedOne);
            if (removed.Count is not 0)
            {
                var removedOne = removed[0];
                var removedOneIndex = (sender as Segmented).Items.IndexOf(removedOne);


                //if the index is greater than the removed index, we are going forward
                if (addedOneIndex >
                    removedOneIndex)
                {
                    info = new Microsoft.UI.Xaml.Media.Animation.SlideNavigationTransitionInfo
                    {
                        Effect = SlideNavigationTransitionEffect.FromLeft
                    };
                }
                else
                {
                    info = new Microsoft.UI.Xaml.Media.Animation.SlideNavigationTransitionInfo
                    {
                        Effect = SlideNavigationTransitionEffect.FromRight
                    };
                }
            }
            else
            {
                // drill in
                info = new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo();
            }

            var addedOneItem = ViewModel.RightSidebar.Items[addedOneIndex];
            switch (addedOneItem)
            {
                case RightSidebarVideoViewModel vv:
                    ViewModel.RightSidebar.Navigation.Navigate(info, vv);
                    break;
                case RightSidebarLyricsViewModel lv:
                    ViewModel.RightSidebar.Navigation.Navigate(info, lv);
                    break;
            }
        }
    }

    public Visibility TrueThenVisible(bool? b)
    {
        return b is true ? Visibility.Visible : Visibility.Collapsed;
    }


    public GridLength TrueThenSavedOrDefault(bool? b)
    {
        if (b is true)
        {
            return new GridLength(ViewModel.Playlists.SidebarWidth, GridUnitType.Pixel);
        }

        return new GridLength(0);
    }

    public GridLength TrueThenSavedOrDefaultRight(bool? b)
    {
        if (b is true)
        {
            return new GridLength(ViewModel.RightSidebar.SidebarWidth, GridUnitType.Pixel);
        }

        return new GridLength(0);
    }


    private void RightSidebarGridChanged(object sender, SizeChangedEventArgs e)
    {
        var grid = sender as Grid;
        var width = grid.ActualWidth;
        ViewModel.RightSidebar.SidebarWidth = width;
    }

    private void PlaylistSidebarChanged(object sender, SizeChangedEventArgs e)
    {
        var grid = sender as Grid;
        var width = grid.ActualWidth;
        ViewModel.Playlists.SidebarWidth = width;
    }
    // private void NavigationFrame_OnSizeChanged(object sender, SizeChangedEventArgs e)
    // {
    //     ViewModel.Playlists.SidebarWidth = PlaylistsGrid.ActualWidth;
    //     ViewModel.RightSidebar.SidebarWidth = RightSidebarGrid.ActualWidth;
    // }

    public Thickness ToWinUIThickness(SharedThickness sharedThickness)
    {
        return new Thickness(
            left: sharedThickness.Left,
            top: sharedThickness.Top,
            right: sharedThickness.Right,
            bottom: sharedThickness.Bottom
        );
    }
}