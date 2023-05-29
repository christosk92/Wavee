using DynamicData;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Linq;
using DynamicData.Binding;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Ids;
using Wavee.Spotify;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.Infrastructure.Traits;
using Wavee.UI.Models;

namespace Wavee.UI.ViewModels;

public sealed class PlaylistsViewModel<R> : ReactiveObject where R : struct, HasSpotify<R>
{
    private readonly ReadOnlyObservableCollection<IPlaylistViewModel> _playlistItemsView;
    private readonly SourceCache<IPlaylistViewModel, string> _items = new(s => s.Id);
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
                            return SortExpressionComparer<IPlaylistViewModel>.Ascending(t => t.Index);
                        case PlaylistSortProperty.Alphabetical:
                            return SortExpressionComparer<IPlaylistViewModel>.Ascending(t => t.Name);
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

                Seq<IPlaylistViewModel> result = LanguageExt.Seq<IPlaylistViewModel>.Empty;
                Option<IPlaylistViewModel> currentFolder = Option<IPlaylistViewModel>.None;
                int index = -1;
                foreach (var playlist in playlists.Contents.Items)
                {
                    try
                    {
                        index++;
                        if (playlist.Uri.StartsWith("spotify:start-group"))
                        {
                            //folder
                            //spotify:start-group:a19bead8a80b545b:New+Folder
                            var split = playlist.Uri.Split(':');
                            var folderName = split[^1];
                            var folderId = split[^2];
                            currentFolder = new PlaylistViewModel(folderId, runtime is WaveeUIRuntime wv ? wv : default,
                                true)
                            {
                                IsInFolder = false,
                                IsFolder = true,
                                Id = folderId,
                                OwnerId = c.Username,
                                SubItems = LanguageExt.Seq<IPlaylistViewModel>.Empty,
                                Revision = playlists.Contents.MetaItems[index].Revision,
                                Timestamp = DateTimeOffset.MinValue,
                                Name = folderName,
                                Index = index
                            };
                            continue;
                        }

                        if (playlist.Uri.StartsWith("spotify:end-group"))
                        {
                            //end of folder
                            //commit folder
                            result = result.Add(currentFolder.ValueUnsafe());
                            currentFolder = Option<IPlaylistViewModel>.None;
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

                            currentFolderValue.AddSubitem(
                                new PlaylistViewModel(playlist.Uri, runtime is WaveeUIRuntime wv ? wv : default)
                                {
                                    IsInFolder = true,
                                    IsFolder = false,
                                    Id = playlist.Uri,
                                    OwnerId = owner,
                                    SubItems = LanguageExt.Seq<IPlaylistViewModel>.Empty,
                                    Revision = playlists.Contents.MetaItems[index].Revision,
                                    Timestamp = timestampAsDatetime,
                                    Index = currentFolderValue.SubItems.Count,
                                    Name = name,
                                });
                        }
                        else
                        {
                            result = result.Add(
                                new PlaylistViewModel(playlist.Uri, runtime is WaveeUIRuntime wv ? wv : default)
                                {
                                    IsInFolder = false,
                                    IsFolder = false,
                                    Id = playlist.Uri,
                                    OwnerId = owner,
                                    SubItems = LanguageExt.Seq<IPlaylistViewModel>.Empty,
                                    Revision = playlists.Contents.MetaItems[index].Revision,
                                    Timestamp = timestampAsDatetime,
                                    Index = index,
                                    Name = name,
                                });
                        }
                    }
                    catch (Exception x)
                    {
                        Debug.WriteLine(x);
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
    public ReadOnlyObservableCollection<IPlaylistViewModel> Playlists => _playlistItemsView;
}