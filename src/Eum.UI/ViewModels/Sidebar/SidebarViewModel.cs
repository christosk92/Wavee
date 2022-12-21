using System.Collections.ObjectModel;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
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
using ReactiveUI;

namespace Eum.UI.ViewModels.Sidebar;
[INotifyPropertyChanged]
public sealed partial class SidebarViewModel
{
    [ObservableProperty]
    private SidebarItemViewModel? _selectedSidebarItem;

    private readonly ObservableCollectionExtended<PlaylistViewModel> _playlists = new ();
    private EumUserViewModel _user;
    private readonly IDisposable _disposable;
    public SidebarViewModel(EumUserViewModel user,
        IEumUserPlaylistViewModelManager eumUserPlaylistViewModelManager)
    {
        _user = user;
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

    public void Deconstruct()
    {
        _disposable?.Dispose();
    }


    public ObservableCollection<ISidebarItem> SidebarItems { get; } = new()
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
        new LibraryViewModel(EntityType.Album),
        new LibraryViewModel(EntityType.Artist),
        new LibraryViewModel(EntityType.Track),
        new LibraryViewModel(EntityType.Show),
        new SidebarPlaylistHeader()
    };

    public ObservableCollectionExtended<PlaylistViewModel> Playlists => _playlists;
}