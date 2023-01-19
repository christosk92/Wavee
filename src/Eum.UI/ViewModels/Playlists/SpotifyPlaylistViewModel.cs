using System.Collections.ObjectModel;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.Connections.Spotify;
using Eum.Spotify.playlist4;
using Eum.UI.Items;
using Eum.UI.Playlists;
using Eum.UI.Services.Tracks;
using Eum.Logging;
using AsyncLock = Nito.AsyncEx.AsyncLock;
using Exception = System.Exception;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Eum.Connections.Spotify.Connection;
using Eum.Enums;
using Eum.UI.Services;
using MoreLinq;
using ReactiveUI;
using Eum.Users;

namespace Eum.UI.ViewModels.Playlists;

public class SpotifyPlaylistViewModel : PlaylistViewModel
{
    private IDisposable _listener;
    public SpotifyPlaylistViewModel(EumPlaylist playlist) : base(playlist)
    {
    }

    private bool _isSorting = false;
    private AsyncLock _syncLock = new AsyncLock();

    public override void Connect()
    {
        base.Connect();
        _listener= Ioc.Default.GetRequiredService<ISpotifyClient>()
            .PlaylistUpdated
            .Where(a => a.Id.Uri == Id.Uri)
            .SelectMany(async a =>
            {
                foreach (var update in a.Updates)
                {
                    if (update is AddItemsToPlaylistUpdate add)
                    {
                        //fetch the tracks
                        var items = add.Items.Select(a => new ItemId(a.Uri)).ToArray();
                        _ = (await Ioc.Default.GetRequiredService<ITrackAggregator>()
                            .GetTracks(items));
                    }
                }

                return a;
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async updates =>
            {
                foreach (var playlistUpdate in updates.Updates)
                {
                    switch (playlistUpdate)
                    {
                        case AddItemsToPlaylistUpdate add:
                            var items = add.Items.Select(a => new ItemId(a.Uri)).ToArray();
                            var tracks = (await Ioc.Default.GetRequiredService<ITrackAggregator>()
                                .GetTracks(items));
                            Tracks.AddRange(ToVm(tracks), add.FromIndex);
                            break;
                        case RemoveItemsFromPlaylistUpdate remove:
                            Tracks.RemoveRange(remove.FromIndex, remove.Length);
                            break;
                        case MoveItemsInPlaylistUpdate move:
                            var newIndex = Math.Max(0, Math.Min(Tracks.Count - 1, move.ToIndex < move.FromIndex ? move.ToIndex : move.ToIndex - 1));
                            var from = move.FromIndex;
                            var length = move.Length;

                            S_Log.Instance.LogInfo($"Moving from {from} with l: {length} to {newIndex}. Actually got: {move.FromIndex}, {move.Length}, {move.ToIndex}");
                            Tracks = new RangeObservableCollection<PlaylistTrackViewModel>(Tracks.Move(move.FromIndex,
                                move.Length, newIndex));
                            // var range = Tracks.Skip(move.FromIndex).Take(move.Length).ToList();
                            // Tracks.RemoveRange(move.FromIndex, move.Length);
                            // await Task.Delay(TimeSpan.FromMilliseconds(50));
                            // int toIndex = move.ToIndex;
                            // if (toIndex > move.FromIndex)
                            //     toIndex -= move.Length;
                            // Tracks.InsertRange(toIndex, range);
                            break;
                    }
                }

                for (var index = 0; index < Tracks.Count; index++)
                {
                    var playlistTrackViewModel = Tracks[index];
                    playlistTrackViewModel.Index = index;
                }
            });
    }

    public override void Disconnect()
    {
        _listener?.Dispose();
        base.Disconnect();
    }

    public override async Task Sync(bool addTracks = false)
    {
        //fetch the data..

        try
        {
            // https://spclient.wg.spotify.com/playlist/v2/playlist/37i9dQZF1E36Y1WP5YoeGC
            using (await _syncLock.LockAsync())
            {
                if (addTracks)
                {
                    //first add the items we already know
                    var filtered = _playlist.Tracks?.Where(a => a.Type != EntityType.Unknown)?.ToArray();
                    if (filtered is
                        {
                            Length: > 0
                        })
                    {
                        var tracks = (await Ioc.Default.GetRequiredService<ITrackAggregator>()
                            .GetTracks(filtered));
                        var asVms = ToVm(tracks.Where(a => a != null));
                        Ioc.Default.GetRequiredService<IDispatcherHelper>()
                            .TryEnqueue(QueuePriority.High, () => { Tracks.AddRange(asVms); });
                    }
                }

                _ = Task.Run(() => FetchNewItems(addTracks));
            }
        }
        catch (Exception x)
        {
            S_Log.Instance.LogError(x);
            throw;
        }
    }

    public override bool CanCollab => true;

    private async Task FetchNewItems(bool addTracks)
    {
        var spotifyClient = Ioc.Default.GetRequiredService<ISpotifyClient>();

        using var playlistResponse =
            await spotifyClient.SpClientPlaylists.GetPlaylist(Playlist.Id.Id, CancellationToken.None);

        var data = SelectedListContent.Parser.ParseFrom(await (playlistResponse.Content.ReadAsStreamAsync()));
        Playlist.Collab = data.Attributes.HasCollaborative && data.Attributes.Collaborative;
        Playlist.Metadata = Playlist.Metadata;

        var level = PlaylistPermissionLevel.Viewer;
        if (_playlist.User == Ioc.Default.GetRequiredService<MainViewModel>().CurrentUser.User.Id)
        {
            level = PlaylistPermissionLevel.Owner;
        }
        else if (Playlist.Collab)
        {
            level = PlaylistPermissionLevel.Collab;
        }

        Ioc.Default.GetRequiredService<IDispatcherHelper>()
            .TryEnqueue(QueuePriority.Normal, () => { PermissionLevel = level; });
        //TODO: merge with local tracks
        var uris = data
            .Contents.Items.Select(a => new ItemId(a.Uri))
            .ToArray();
        var tracks = (await Ioc.Default.GetRequiredService<ITrackAggregator>()
            .GetTracks(uris));
        if (addTracks)
        {
            var assARr = ToVm(tracks)
                .ToArray();
            Ioc.Default.GetRequiredService<IDispatcherHelper>()
                .TryEnqueue(QueuePriority.High, () =>
                {
                    Tracks = new RangeObservableCollection<PlaylistTrackViewModel>(assARr);
                    // //I have a list of items, and i have a new incoming list with new items in c#
                    // //I want to diff the lists and add,remove,move items in the original list based on the new incoming list
                    // //// items to remove
                    // var removedItems = Tracks.ExceptBy(assARr.Select(a=>(a.OriginalIndex, a.Id)), model => (model.OriginalIndex, model.Id)).ToArray();
                    // foreach (var item in removedItems)
                    //     Tracks.Remove(item);
                    //
                    // // items to add
                    // var addedItems = assARr.ExceptBy(Tracks.Select(a => (a.OriginalIndex, a.Id)), model => (model.OriginalIndex, model.Id)).ToArray();
                    // foreach (var item in addedItems)
                    //     Tracks.Add(item);
                    // //
                    // // // check for moved items
                    // // for (int i = 0; i < Tracks.Count; i++)
                    // // {
                    // //     if (!Tracks[i].Equals(assARr[i]))
                    // //     {
                    // //         // item has been moved
                    // //         // perform necessary actions
                    // //         Tracks.Move(i, Tracks.IndexOf(Tracks[i]));
                    // //     }
                    // // }
                });
        }

        Ioc.Default.GetRequiredService<IDispatcherHelper>()
            .TryEnqueue(QueuePriority.High, () => { Playlist.Tracks = uris; });
    }

    private IEnumerable<PlaylistTrackViewModel> ToVm(IEnumerable<EumTrack> tracks)
    {
        var main = Ioc.Default.GetRequiredService<MainViewModel>();
        var playCommand = new AsyncRelayCommand<int>(async index =>
        {
            try
            {
                if (_isSorting)
                {
                    var allPages = Tracks.Select(a => new PagedTrack(a.Id, new Dictionary<string, string>
                    {
                        {"original_index", _playlist.Tracks.IndexOf(a.Id).ToString()}
                    }));
                    await Ioc.Default.GetRequiredService<IPlaybackService>()
                        .PlayOnDevice(new PagedContextPlayCommand(Id,
                            allPages.ToArray(),
                            index, Playlist.Metadata.ToDictionary(a => a.Key, a => a.Value)));
                }
                else
                {
                    await Ioc.Default.GetRequiredService<IPlaybackService>()
                        .PlayOnDevice(new PlainContextPlayCommand(Id,
                            index, Playlist.Metadata.ToDictionary(a => a.Key, a => a.Value)));
                }
            }
            catch (Exception notImplementedException)
            {
                await Ioc.Default.GetRequiredService<IErrorMessageShower>()
                    .ShowErrorAsync(notImplementedException, "Unexpected error",
                        "Something went wrong while trying to play the track.");
            }
        });

        return tracks.Select((a, i) => new PlaylistTrackViewModel(a, i, playCommand)
        {
            IsSaved = main.CurrentUser.User.LibraryProvider.IsSaved(a.Id),
        });
    }
}