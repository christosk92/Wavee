using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using LanguageExt;
using ReactiveUI;
using Wavee.UI.Bases;
using Wavee.UI.Users;
using Wavee.UI.ViewModels.Playlist;
using Wavee.UI.ViewModels.Sidebar;

namespace Wavee.UI.ViewModels;

public sealed class MainViewModel : ReactiveObject
{
    private readonly ReadOnlyObservableCollection<PlaylistViewModel> _playlistItemsView;
    private readonly SourceCache<PlaylistViewModel, string> _items = new(s => s.Id);
    private PlaylistSortProperty _playlistSorts;
    private User _currentUser;
    private Seq<PlaylistSourceFilter> _playlistSourceFilters;
    public static UiConfig UiConfig => Services.UiConfig;
    public IReadOnlyList<AbsSidebarItemViewModel> SidebarItems { get; }

    public ReadOnlyObservableCollection<PlaylistViewModel> Playlists => _playlistItemsView;
    public MainViewModel()
    {
        SidebarItems = new AbsSidebarItemViewModel[]
        {
            new HeaderSidebarItem { Title = "For You" },
            new RegularSidebarItem
            {
                Icon = "\uE10F",
                IconFontFamily = "Segoe MDL2 Assets",
                Title = "Home"
            },
            new RegularSidebarItem
            {
                Icon = "\uE794",
                IconFontFamily = "/Assets/Fonts/MediaPlayerIcons.ttf#Media Player Fluent Icons",
                Title = "Browse"
            },
            new HeaderSidebarItem { Title = "Library" },
            new CountedSidebarItem
            {
                Icon = "\uE00B",
                IconFontFamily = "Segoe MDL2 Assets",
                Title = "Songs",
                Count = 0
            },
            new CountedSidebarItem
            {
                Icon = "\uE93C",
                IconFontFamily = "Segoe MDL2 Assets",
                Title = "Albums",
                Count = 0
            },
            new CountedSidebarItem
            {
                Icon = "\uEBDA",
                IconFontFamily = "Segoe MDL2 Assets",
                Title = "Artists",
                Count = 0
            },
            new CountedSidebarItem
            {
                Icon = "\uEB44",
                IconFontFamily = "Segoe MDL2 Assets",
                Title = "Podcasts",
                Count = 0
            }
        };
        PlaylistSorts = UiConfig.PlaylistSortProperty;


        var sortExpressionObservable =
            this.WhenAnyValue(x => x.PlaylistSorts)
                .Select(sortProperty =>
                {
                    UiConfig.PlaylistSortProperty = sortProperty;
                    switch (sortProperty)
                    {
                        case PlaylistSortProperty.Created:
                            return SortExpressionComparer<PlaylistViewModel>.Ascending(t => t.CreatedAt);
                        case PlaylistSortProperty.RecentlyPlayed:
                            return SortExpressionComparer<PlaylistViewModel>.Ascending(t => t.LastPlayedAt);
                        case PlaylistSortProperty.Custom:
                            return SortExpressionComparer<PlaylistViewModel>.Ascending(t => t.Index);
                        case PlaylistSortProperty.Alphabetical:
                            return SortExpressionComparer<PlaylistViewModel>.Ascending(t => t.Name);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(sortProperty), sortProperty, null);
                    }
                });

        var filterExpressionObservable =
            this.WhenAnyValue(x => x.PlaylistSourceFilters)
                .Select(filterProperty =>
                {
                    return (Func<PlaylistViewModel, bool>)(t => (filterProperty.Contains(PlaylistSourceFilter.Yours) && t.OwnerId == CurrentUser.Id) ||
                                                                (filterProperty.Contains(PlaylistSourceFilter.Others) && t.OwnerId != CurrentUser.Id));
                });

        _items
            .Connect()
            .Sort(sortExpressionObservable)
            .Filter(filterExpressionObservable)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _playlistItemsView)
            .Subscribe();

        HasPlaylists = _items
            .Connect()
            .CountChanged() // emits the count of items whenever it changes
            .Select(count => count.Count > 0) // maps the count to a boolean
            .StartWith(false) // emit an initial value
            .ObserveOn(RxApp.MainThreadScheduler); // ensure that subscribers receive notifications on the main thread

        Instance = this;
        //invoke HasPlaylists
    }
    public static MainViewModel Instance { get; private set; }
    public IObservable<bool> HasPlaylists { get; }
    public IObservable<bool> UserIsLoggedIn => this
        .WhenAnyValue(x => x.CurrentUser)
        .Select(user => user is { IsLoggedIn: true })
        .StartWith(false);

    public PlaylistSortProperty PlaylistSorts
    {
        get => _playlistSorts;
        set => this.RaiseAndSetIfChanged(ref _playlistSorts, value);
    }

    public User CurrentUser
    {
        get => _currentUser;
        set => this.RaiseAndSetIfChanged(ref _currentUser, value);
    }

    public Seq<PlaylistSourceFilter> PlaylistSourceFilters
    {
        get => _playlistSourceFilters;
        set => this.RaiseAndSetIfChanged(ref _playlistSourceFilters, value);
    }
}

public enum PlaylistSortProperty
{
    Custom,
    Created,
    Alphabetical,
    RecentlyPlayed
}

public enum PlaylistSourceFilter
{
    Yours,
    Others,
    Spotify,
    Local
}