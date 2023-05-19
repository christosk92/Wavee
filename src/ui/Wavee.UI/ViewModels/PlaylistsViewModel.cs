using DynamicData;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData.Binding;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Ids;
using Wavee.Spotify;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.Infrastructure.Traits;
using Wavee.UI.Models;

namespace Wavee.UI.ViewModels;

public sealed class PlaylistsViewModel<R> : ReactiveObject where R : struct, HasSpotify<R>
{
    private readonly ReadOnlyObservableCollection<PlaylistInfo> _playlistItemsView;
    private readonly SourceCache<PlaylistInfo, string> _items = new(s => s.Id);
    private PlaylistSortProperty _playlistSort = PlaylistSortProperty.CustomIndex;
    public PlaylistsViewModel(R runtime)
    {
        var sortExpressionObservable =
            this.WhenAnyValue(x => x.PlaylistSort)
                .Select(sortProperty =>
                {
                    switch (sortProperty)
                    {
                        case PlaylistSortProperty.CustomIndex:
                            return SortExpressionComparer<PlaylistInfo>.Ascending(t => t.Index);
                        case PlaylistSortProperty.Alphabetical:
                            return SortExpressionComparer<PlaylistInfo>.Ascending(t => t.Name);
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


        //Fetch initial playlists, and setup a listener for future changes
        Spotify<R>.ObserveRootlist()
            .Run(runtime)
            .ThrowIfFail()
            .ValueUnsafe()
            .SelectMany(async c =>
            {
                var playlists = (await Spotify<R>
                    .GetRootList()
                    .Run(runtime))
                    .ThrowIfFail();

                Seq<PlaylistInfo> result = LanguageExt.Seq<PlaylistInfo>.Empty;
                Option<PlaylistInfo> currentFolder = Option<PlaylistInfo>.None;
                int index = -1;
                foreach (var playlist in playlists.Contents.Items)
                {
                    index++;
                    if (playlist.Uri.StartsWith("spotify:start-group"))
                    {
                        //folder
                        //spotify:start-group:a19bead8a80b545b:New+Folder
                        var split = playlist.Uri.Split(':');
                        var folderName = split[^1];
                        var folderId = split[^2];
                        currentFolder = new PlaylistInfo(
                            Id: folderId,
                            Index: index,
                            Name: folderName,
                            OwnerId: c.Username,
                            IsFolder: true,
                            SubItems: LanguageExt.Seq<PlaylistInfo>.Empty,
                            Timestamp: DateTimeOffset.MinValue,
                            isInFolder: false
                            );
                        continue;
                    }
                    if (playlist.Uri.StartsWith("spotify:end-group"))
                    {
                        //end of folder
                        //commit folder
                        result = result.Add(currentFolder.ValueUnsafe());
                        currentFolder = Option<PlaylistInfo>.None;
                        continue;
                    }

                    var name = playlists.Contents.MetaItems[index].Attributes
                        .Name;
                    var owner = playlists.Contents.MetaItems[index].OwnerUsername;
                    var timestamp = playlists.Contents.MetaItems[index].Timestamp;
                    var timestampAsDatetime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp / 1000);
                    if (currentFolder.IsSome)
                    {
                        var currentFolderValue = currentFolder.ValueUnsafe();

                        currentFolder = currentFolderValue.AddSubitem(new PlaylistInfo(
                            Id: playlist.Uri,
                            Index: currentFolderValue.SubItems.Count,
                            Name: name,
                            OwnerId: owner,
                            IsFolder: false,
                            SubItems: LanguageExt.Seq<PlaylistInfo>.Empty,
                            Timestamp: timestampAsDatetime,
                            isInFolder: true
                        ));
                    }
                    else
                    {
                        result = result.Add(new PlaylistInfo(
                            Id: playlist.Uri,
                            Index: index,
                            Name: name,
                            OwnerId: owner,
                            IsFolder: false,
                            SubItems: LanguageExt.Seq<PlaylistInfo>.Empty,
                            Timestamp: timestampAsDatetime,
                            isInFolder: false
                            ));
                    }
                }
                return result;
            })
            .Subscribe(playlist =>
            {
                _items.Edit(innerList =>
                {
                    innerList.Clear();
                    innerList.AddOrUpdate(playlist);
                });
            });
    }
    public PlaylistSortProperty PlaylistSort
    {
        get => _playlistSort;
        set => this.RaiseAndSetIfChanged(ref _playlistSort, value);
    }
    public ReadOnlyObservableCollection<PlaylistInfo> Playlists => _playlistItemsView;
}