using System.Collections.ObjectModel;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using DynamicData;
using DynamicData.Alias;
using DynamicData.Binding;
using Eum.Enums;
using Eum.UI.Items;
using Eum.UI.Services.Playlists;
using Eum.UI.Users;
using Eum.UI.ViewModels.ForYou;
using Eum.UI.ViewModels.Library;
using Eum.UI.ViewModels.Playlists;
using Eum.UI.ViewModels.Search;
using Eum.Users;
using ReactiveUI;

namespace Eum.UI.ViewModels.Sidebar;
[INotifyPropertyChanged]
public sealed partial class SidebarViewModel
{
    [ObservableProperty]
    private SidebarItemViewModel? _selectedSidebarItem;

    private readonly ObservableCollectionExtended<PlaylistViewModel> _playlists = new();
    private EumUserViewModel _user;
    private readonly IDisposable _disposable;
    public SidebarViewModel(EumUserViewModel user,
        IEumUserPlaylistViewModelManager eumUserPlaylistViewModelManager)
    {
        _user = user;
        SidebarItems =new()
        {
            new SidebarItemHeader
            {
                Title = "For you"
            },
            new HomeViewModel(),
            new ForYouViewModel(),
            new SidebarItemHeader
            {
                Title = "Library"
            },
            new LibraryViewModel(EntityType.Album, user.User)
            {
                LibraryCount = 0
            },
            new LibraryViewModel(EntityType.Artist, user.User) {
                LibraryCount = 0
            },
            new LibraryViewModel(EntityType.Track, user.User) {
                LibraryCount = 0
            },
            new LibraryViewModel(EntityType.Show, user.User)
            {
                LibraryCount = 0
            },
            new SidebarPlaylistHeader()
        };
        Task.Run(async() => await GetCountsFoLbVm());
        foreach (var s in SidebarItems.Where(a => a is LibraryViewModel))
        {
            (s as LibraryViewModel)!.RegisterEvents();
        }
        // foreach (var playlist in eumUserPlaylistViewModelManager
        //              .Playlists.Where(a => a.Playlist.User == user.User.Id || a.Playlist.AlsoUnder.Contains(user.User.Id)))
        // {
        //     SidebarItems.Add(playlist);
        // }
        var indexOfHeader = SidebarItems.ToList().FindIndex(a => a is SidebarPlaylistHeader);

        _disposable = eumUserPlaylistViewModelManager.SourceList
            .Connect()
            .Where(a => a.Playlist.User == user.User.Id || a.Playlist.AlsoUnder.Contains(user.User.Id))
            .Sort(SortExpressionComparer<PlaylistViewModel>
                .Descending(i => i.Playlist.Order))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(_playlists)
            .Subscribe();
        // .Select(async set =>
        // {
        //     foreach (var change in set)
        //     {
        //         switch (change.Reason)
        //         {
        //             case ListChangeReason.AddRange:
        //                 foreach (var playlistViewModel in change.Range)
        //                 {
        //                     SidebarItems.Add(playlistViewModel);
        //                 }
        //                 break;
        //             case ListChangeReason.Add:
        //                 SidebarItems.Insert(indexOfHeader + 1, change.Item.Current);
        //                 break;
        //             case ListChangeReason.Replace:
        //                 break;
        //             case ListChangeReason.Remove:
        //                 await Task.Delay(10);
        //                 SidebarItems.Remove(change.Item.Current);
        //                 break;
        //             case ListChangeReason.RemoveRange:
        //                 foreach (var removed in change.Range)
        //                 {
        //                     SidebarItems.Remove(removed);
        //                 }
        //                 break;
        //             case ListChangeReason.Refresh:
        //
        //                 break;
        //             case ListChangeReason.Moved:
        //
        //                 break;
        //             case ListChangeReason.Clear:
        //
        //                 break;
        //             default:
        //                 throw new ArgumentOutOfRangeException();
        //         }
        //     }
        // })
        // .Subscribe();

    }

    private async Task GetCountsFoLbVm()
    {
        var album = await _user.User.LibraryProvider.LibraryCount(EntityType.Album);
        var track = await _user.User.LibraryProvider.LibraryCount(EntityType.Track);
        var show = await _user.User.LibraryProvider.LibraryCount(EntityType.Show);
        var artist = await _user.User.LibraryProvider.LibraryCount(EntityType.Artist);

        Ioc.Default.GetRequiredService<IDispatcherHelper>()
            .TryEnqueue(QueuePriority.Normal, () =>
            {
                foreach (var sidebarItem in SidebarItems)
                {
                    if (sidebarItem is LibraryViewModel l)
                    {
                        l.LibraryCount = l.LibraryType switch
                        {
                            EntityType.Track => track,
                            EntityType.Album => album,
                            EntityType.Show => show,
                            EntityType.Artist => artist,
                            _ => l.LibraryCount
                        };
                    }
                }
            });
    }
    public void Deconstruct()
    {
        foreach (var s in SidebarItems.Where(a => a is LibraryViewModel))
        {
            (s as LibraryViewModel)!.UnregisterEvents();
        }
        _disposable?.Dispose();
    }


    public ObservableCollection<ISidebarItem> SidebarItems { get; }

    public ObservableCollectionExtended<PlaylistViewModel> Playlists => _playlists;
}