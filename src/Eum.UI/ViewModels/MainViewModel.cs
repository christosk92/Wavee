using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ColorThiefDotNet;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Eum.UI.Helpers;
using Eum.UI.Items;
using Eum.UI.Services;
using Eum.UI.Services.Login;
using Eum.UI.Services.Users;
using Eum.UI.Users;
using Eum.UI.ViewModels.Fullscreen;
using Eum.UI.ViewModels.Navigation;
using Eum.UI.ViewModels.Playback;
using Eum.UI.ViewModels.Search;
using Eum.UI.ViewModels.Search.Sources;
using Nito.AsyncEx;
using ReactiveUI;
using Color = System.Drawing.Color;
using SearchBarViewModel = Eum.UI.ViewModels.Search.SearchBarViewModel;

namespace Eum.UI.ViewModels
{
    [INotifyPropertyChanged]
    public partial class MainViewModel
    {
        [ObservableProperty]
        private PlaybackViewModel? _playbackViewModel;
        private EumUserViewModel _currentUser;
        [ObservableProperty] private LyricsViewModel? _lyricsViewModel;
        [ObservableProperty] private string _glaze;
        [ObservableProperty] private AbsFullscreenViewModel? _fullscreenViewModel;
        [ObservableProperty] private bool _shouldShowSidePanel;
        [ObservableProperty] private string _sidePanelView;
        public MainViewModel(IEumUserViewModelManager userViewModelManager)
        {
            UserViewModelManager = userViewModelManager;
            MainScreen = new NavigationService();
            SearchBar = CreateSearchBar();
            Observable
                .FromEventPattern<AbsFullscreenViewModel>(NavigationService.Instance, nameof(NavigationService.OnFullscreenNavigation))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x => x.EventArgs as AbsFullscreenViewModel)
                .Subscribe(fs =>
                {
                    FullscreenViewModel = fs;
                });
            Observable
                .FromEventPattern<EumUserViewModel>(userViewModelManager, nameof(IEumUserViewModelManager.CurrentUserChanged))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x => x.EventArgs as EumUserViewModel)
                .Subscribe(user =>
                {
                    if (PlaybackViewModel != null)
                    {
                        PlaybackViewModel.Deconstruct();
                    }

                    if (CurrentUser != null)
                    {
                        CurrentUser.User.ThemeService.GlazeChanged -= ThemeServiceOnGlazeChanged;
                    }
                    CurrentUser = user;
                    if (PlaybackViewModel != null)
                    {
                        PlaybackViewModel.Deconstruct();
                    }
                    PlaybackViewModel = user.User.Id.Service switch
                    {
                        ServiceType.Spotify => Ioc.Default.GetRequiredService<IEnumerable<PlaybackViewModel>>().FirstOrDefault(a => a.Service == ServiceType.Spotify)
                    };
                    PlaybackViewModel.Construct();
                    Glaze = user.User.Accent switch
                    {
                        "System Color" => user.User.ThemeService.Glaze,
                        "Page Dependent" => "#00000000",
                        "Playback Dependent" => AsyncContext.Run(GetColorFromAlbumArt),
                        _ => user.User.ThemeService.Glaze
                    };
                    CurrentUser.User.ThemeService.GlazeChanged += ThemeServiceOnGlazeChanged;

                });
        }
        public SearchBarViewModel SearchBar { get; }

        private async void ThemeServiceOnGlazeChanged(object sender, string e)
        {
            Glaze = CurrentUser.User.Accent switch
            {
                "System Color" => e,
                "Page Dependent" => "#00000000",
                "Playback Dependent" => await GetColorFromAlbumArt(),
                _ => e
            };
        }

        private async Task<string> GetColorFromAlbumArt()
        {
            if (PlaybackViewModel.Item?.BigImageUrl != null)
            {
                return await Task.Run(async () =>
                {
                    using var fs = await Ioc.Default.GetRequiredService<IFileHelper>()
                        .GetStreamForString(PlaybackViewModel.Item?.BigImageUrl.ToString(), default);
                    using var bmp = new Bitmap(fs);
                    var colorThief = new ColorThief();
                    var c = colorThief.GetPalette(bmp);
                    var a =
                        c[0].Color.ToHexString();

                    var f = a.ToColor();
                    return (Color.FromArgb(25, f.R, f.G, f.B)).ToHex();
                });

            }

            return "#00000000";
        }

        public NavigationService MainScreen { get; }
        public IEumUserViewModelManager UserViewModelManager { get; }

        public EumUserViewModel CurrentUser
        {
            get => _currentUser;
            set => this.SetProperty(ref _currentUser, value);
        }


        [RelayCommand]
        public void ShowSidePanel(string panelType)
        {
            if (SidePanelView == panelType)
            {
                //close
                SidePanelView = null;
                ShouldShowSidePanel = false;
            }
            else
            {
                SidePanelView = panelType;
                ShouldShowSidePanel = true;
                if (panelType == "lyrics")
                {
                    LyricsViewModel = new LyricsViewModel(Ioc.Default.GetRequiredService<ILyricsProvider>(),
                        PlaybackViewModel, Ioc.Default.GetRequiredService<IDispatcherHelper>());
                }
                else
                {
                    LyricsViewModel = null;
                }
            }
        }

        private SearchBarViewModel CreateSearchBar()
        {
            // This subject is created to solve the circular dependency between the sources and SearchBarViewModel
            var filterChanged = new Subject<string>();

            var source = new CompositeSearchSource(
                new SpotifySearchSource(filterChanged));

            var searchBar = new SearchBarViewModel(
                source.IsBusy,
                source.Changes, source.GroupChanges);

            searchBar
                .WhenAnyValue(a => a.SearchText)
                .Throttle(TimeSpan.FromMilliseconds(100))
                .WhereNotNull()
                .Subscribe(filterChanged);

            return searchBar;
        }
    }
}
