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
using Wavee.UI.Features.Search.ViewModels;
using Wavee.UI.Features.Shell.ViewModels;
using Wavee.UI.WinUI.Services;
using Wavee.UI.WinUI.Views.Search;
using NavigationTransitionInfo = Microsoft.UI.Xaml.Media.Animation.NavigationTransitionInfo;
using UserControl = Microsoft.UI.Xaml.Controls.UserControl;

namespace Wavee.UI.WinUI.Views.Shell
{
    public sealed partial class ShellView : UserControl
    {
        public ShellView()
        {
            this.InitializeComponent();
        }

        public ShellViewModel ViewModel => DataContext is ShellViewModel vm ? vm : null;

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
        private void ShellView_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
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
                var navService = new WinUINavigationService(Constants.ServiceProvider);
                ViewModel.RightSidebar.Navigation = navService;
                navService.Initialize(RightSidebarNavigationFrame);
                _initialized = true;
            }
        }

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
                ViewModel.Navigation.NavigateToArtist(entity.Id);
            }
        }

        private void FrameworkElement_OnLayoutUpdated(object sender, object e)
        {
            // if (SuggestBox is not null)
            //     SuggestBox.IsSuggestionListOpen = true;
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
            // if (o is RightSidebarItemViewModel item)
            // {
            //     ViewModel.RightSidebar.SelectedItem = item;
            // }
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
    }
}
