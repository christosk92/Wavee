using System.Windows.Forms;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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

        private void ShellView_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (ViewModel?.Navigation is WinUINavigationService nav)
            {
                nav.Initialize(NavigationFrame);
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
                entity.Navigate(ViewModel.Navigation, ViewModel.Mediator);
            }
        }

        private void FrameworkElement_OnLayoutUpdated(object sender, object e)
        {
            // if (SuggestBox is not null)
            //     SuggestBox.IsSuggestionListOpen = true;
        }
    }
}
