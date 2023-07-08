using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using ReactiveUI;
using Wavee.Id;
using Wavee.UI.Helpers;
using Wavee.UI.User;
using Wavee.UI.ViewModel.Shell.Sidebar;

namespace Wavee.UI.ViewModel.Playlist.User;

public sealed class PlaylistsViewModel : ObservableObject, IDisposable
{
    private readonly SourceCache<PlaylistInfo, string> _playlists = new(x => x.Id);
    private readonly CompositeDisposable _playlistListener;

    public PlaylistsViewModel(UserViewModel user,
        BulkConcurrentObservableCollection<ISidebarItem> sidebaritems)
    {
        var offset = sidebaritems.Count;
        _playlistListener = new CompositeDisposable();
        user
            .Client.Playlist.ListenForUserPlaylists()
            .Subscribe(x =>
            {
                _playlists.Edit(innerList =>
                {
                    switch (x.ChangeType)
                    {
                        case PlaylistInfoChangeType.Add:
                            innerList.AddOrUpdate(x.Playlists);
                            break;
                        case PlaylistInfoChangeType.Remove:
                            innerList.Remove(x.Playlists);
                            break;
                        case PlaylistInfoChangeType.Refresh:
                            innerList.Clear();
                            innerList.AddOrUpdate(x.Playlists);
                            break;
                    }
                });
            })
            .DisposeWith(_playlistListener);

        _playlists.Connect()
            .Transform(x => x.IsFolder ?
                new PlaylistFolderSidebarItem(title: x.Name, isExpanded: false,
                    playlists: x.Children.Select(ToSidebarItem).ToArray())
                : ToSidebarItem(x) as ISidebarItem)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Select(x =>
            {
                var allUpdates = x.All(z => z.Reason is ChangeReason.Update);
                if (allUpdates)
                {
                    //remove all
                    sidebaritems.RemoveRange(offset, sidebaritems.Count - offset);
                    //add all
                    sidebaritems.Add(x.Select(z => z.Current));
                }
                else
                {
                    foreach (var change in x)
                    {
                        switch (change.Reason)
                        {
                            case ChangeReason.Add:
                                if (change.CurrentIndex is -1)
                                {
                                    sidebaritems.Add(change.Current);
                                }
                                else
                                {
                                    sidebaritems.Insert(offset + change.CurrentIndex, change.Current);
                                }

                                break;
                            case ChangeReason.Remove:
                                sidebaritems.Remove(change.Current);
                                break;
                        }
                    }
                }

                return Unit.Default;
            })
            .Subscribe()
            .DisposeWith(_playlistListener);
    }

    private PlaylistSidebarItem ToSidebarItem(PlaylistInfo playlistInfo)
    {
        return new PlaylistSidebarItem(
            title: playlistInfo.Name,
            iconGlyph: null,
            iconFontFamily: null,
            viewModelType: typeof(PlaylistsViewModel),
            parameter: playlistInfo.Id
        );
    }


    public IObservable<InternalPlaylistInfoNotification> Notifications => _playlists
        .Connect()
        .SelectMany(x =>
        {
            var additions = x.Where(y => y.Reason == ChangeReason.Add);
            var removals = x.Where(y => y.Reason == ChangeReason.Remove);
            return additions.Select(y => new InternalPlaylistInfoNotification
            {
                PlaylistInfo = y.Current,
                ChangeType = PlaylistInfoChangeType.Add
            }).Concat(removals.Select(y => new InternalPlaylistInfoNotification
            {
                PlaylistInfo = y.Current,
                ChangeType = PlaylistInfoChangeType.Remove
            }));
        })
        .StartWith(_playlists.Items.Select(x => new InternalPlaylistInfoNotification
        {
            PlaylistInfo = x,
            ChangeType = PlaylistInfoChangeType.Add
        }));

    public void Dispose()
    {
        _playlists.Dispose();
        _playlistListener.Dispose();
    }
}

public sealed class InternalPlaylistInfoNotification
{
    public required PlaylistInfo PlaylistInfo { get; set; }
    public required PlaylistInfoChangeType ChangeType { get; set; }
}

public enum PlaylistInfoChangeType
{
    Add,
    Remove,
    Refresh
}