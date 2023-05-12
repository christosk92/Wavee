using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Wavee.UI.Bases;
using Wavee.UI.ViewModels.Playlist;
using Wavee.UI.ViewModels.Sidebar;

namespace Wavee.UI.ViewModels;

public sealed class MainViewModel : ReactiveObject
{
    private readonly ReadOnlyObservableCollection<PlaylistViewModel> _playlistItemsView;
    private readonly SourceCache<PlaylistViewModel, string> _items = new(s => s.Id);
    private PlaylistSortProperty _playlistSorts;
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


        _items
            .Connect() 
            .Sort(sortExpressionObservable)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _playlistItemsView)
            .Subscribe();
    }

    public PlaylistSortProperty PlaylistSorts
    {
        get => _playlistSorts;
        set => this.RaiseAndSetIfChanged(ref _playlistSorts, value);
    }
}

public enum PlaylistSortProperty
{
    Custom,
    Created,
    Alphabetical,
    RecentlyPlayed
}