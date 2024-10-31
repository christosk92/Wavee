using System.Collections.ObjectModel;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using Eum.Spotify.playlist4;
using Microsoft.Extensions.Logging;
using Wavee.Interfaces;
using Wavee.Models.Common;
using Wavee.Models.Playlist;
using Wavee.Services.Playback.Remote;
using AsyncLock = NeoSmart.AsyncLock.AsyncLock;

namespace Wavee.Services.Playlists;

internal sealed class SpotifyPlaylistClient : ISpotifyPlaylistClient, IDisposable
{
    private readonly ReplaySubject<bool> _initialized = new();
    private readonly ISpotifyApiClient _api;
    private readonly ISpotifyPlaylistRepository _playlistRepository;
    private readonly ILogger<SpotifyPlaylistClient> _logger;

    private readonly AsyncLock _selectedListLock = new();

    private readonly SourceCache<SpotifyPlaylistOrFolder, SpotifyId> _items =
        new SourceCache<SpotifyPlaylistOrFolder, SpotifyId>(x => x.Id);

    private readonly ReadOnlyObservableCollection<SpotifyPlaylistOrFolder> _playlists;
    private readonly ISpotifyWebsocketState _state;

    public SpotifyPlaylistClient(
        ISpotifyApiClient apiValue,
        ISpotifyWebsocketState websocketStateValue,
        ISpotifyPlaylistRepository playlistRepository,
        ILoggerFactory loggerFactory)
    {
        _state = websocketStateValue ?? throw new ArgumentNullException(nameof(websocketStateValue));
        var logger = loggerFactory.CreateLogger<SpotifyPlaylistClient>();
        _api = apiValue ?? throw new ArgumentNullException(nameof(apiValue));
        _playlistRepository = playlistRepository ?? throw new ArgumentNullException(nameof(playlistRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _items.Connect()
            .AutoRefresh(x => x.Index)
            .Sort(SortExpressionComparer<SpotifyPlaylistOrFolder>.Ascending(x => x.Index))
            .ObserveOn(TaskPoolScheduler.Default)
            .Bind(out _playlists)
            .Subscribe();

        websocketStateValue.AddMessageHandler("hm://playlist/v2/user/{user_id}/rootlist", HandleRootlistChanged);
    }

    public ReadOnlyObservableCollection<SpotifyPlaylistOrFolder> Playlists => _playlists;
    public IObservable<bool> Initialized => _initialized.DistinctUntilChanged();

    public async Task<SpotifyPlaylist> GetPlaylist(SpotifyId id, CancellationToken cancellationToken)
    {
        var cachedPlaylist = await _playlistRepository.GetPlaylist(id.ToString());
        if (cachedPlaylist is null)
        {
            // Log: Could not find cached playlist with ID: {Id}, fetching from API instead
            _logger.LogWarning("Could not find cached playlist with ID: {Id}, fetching from API instead", id);
            var selectedListContent = await _api.GetPlaylist(null, id, cancellationToken);
            if (selectedListContent is null)
            {
                // Log: Could not find playlist with ID: {Id}
                _logger.LogWarning("Could not find playlist with ID: {Id}", id);
                return null;
            }

            var metaItem = selectedListContent;
            var playlistName = WebUtility.UrlDecode(metaItem.Attributes.Name);
            var playlistDescription = metaItem.Attributes.Description ?? string.Empty;

            var tracks = new List<SpotifyPlaylistTrack>(metaItem.Length);
            for (int i = 0; i < metaItem.Length; i++)
            {
                var pseudoTrack = new SpotifyPlaylistTrack();
                pseudoTrack.Index = i;
                pseudoTrack.OriginalIndex = i;
                tracks.Add(pseudoTrack);
            }

            var playlist = new SpotifyPlaylist(
                id: id,
                name: playlistName,
                index: -1,
                addedAt: DateTimeOffset.FromUnixTimeMilliseconds(metaItem.Timestamp),
                revision: metaItem.Revision.ToBase64(),
                _state,
                tracks,
                _api,
                _playlistRepository,
                _logger
            );

            var cachedPlaylistItem = CreatePlaylistItem(playlist);
            await _playlistRepository.SavePlaylist(cachedPlaylistItem);
            return playlist;
        }
        
        return CreateFrom(cachedPlaylist);
    }

    private async Task HandleRootlistChanged(SpotifyWebsocketMessage message, IDictionary<string, string> parameters,
        CancellationToken cancellationtoken)
    {
        var initialized = await _initialized.Take(1).ToTask(cancellationtoken);
        if (!initialized)
            return;

        using (await _selectedListLock.LockAsync())
        {
            var syncToken = await _api.RetrieveSyncTokenAsync("rootlist", cancellationtoken);
            var playlists = await _api.GetRootListAsync(syncToken, cancellationToken: cancellationtoken);
            if (playlists.Diff is not null && !string.IsNullOrEmpty(syncToken))
            {
                await DoDiff(playlists.Diff);
            }
            else
            {
                var structure = CreateStructure(playlists);
                _items.Edit(innerList =>
                {
                    innerList.Clear();
                    innerList.AddOrUpdate(structure);
                });
                await _playlistRepository.SaveCachedPlaylists(CreateCachedPlaylistItems(_items.Items));
                await _api.StoreSyncTokenAsync("rootlist", playlists.Revision.ToBase64(), cancellationtoken);
            }
        }
    }

    public async Task Initialize(CancellationToken cancellationToken = default)
    {
        using (await _selectedListLock.LockAsync())
        {
            var syncToken = await _api.RetrieveSyncTokenAsync("rootlist", cancellationToken);
            var playlists = await _api.GetRootListAsync(syncToken, cancellationToken: cancellationToken);
            if (playlists.Diff is not null && !string.IsNullOrEmpty(syncToken))
            {
                // Load playlists from cache
                var initialPlaylists = await _playlistRepository.GetCachedPlaylists();
                var structure = CreateStructure(initialPlaylists);
                _items.Edit(innerList =>
                {
                    innerList.Clear();
                    innerList.AddOrUpdate(structure);
                });
                await DoDiff(playlists.Diff);
            }
            else
            {
                var structure = CreateStructure(playlists);
                _items.Edit(innerList =>
                {
                    innerList.Clear();
                    innerList.AddOrUpdate(structure);
                });
                await _playlistRepository.SaveCachedPlaylists(CreateCachedPlaylistItems(_items.Items));
                await _api.StoreSyncTokenAsync("rootlist", playlists.Revision.ToBase64(), cancellationToken);
            }

            _initialized.OnNext(true);
        }
    }

    private async Task DoDiff(Diff playlistsDiff)
    {
        var targetRevision = playlistsDiff.ToRevision.ToBase64();
        //TODO: Actual diff the items
        foreach (var op in playlistsDiff.Ops)
        {
            switch (op.Kind)
            {
                case Op.Types.Kind.Unknown:
                    break;
                case Op.Types.Kind.Add:
                    HandleAddOp(op.Add);
                    break;
                case Op.Types.Kind.Rem:
                    HandleRemoveOp(op.Rem);
                    break;
                case Op.Types.Kind.Mov:
                    HandleMoveOp(op.Mov);
                    break;
                case Op.Types.Kind.UpdateItemAttributes:
                    break;
                case Op.Types.Kind.UpdateListAttributes:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        await _playlistRepository.SaveCachedPlaylists(CreateCachedPlaylistItems(_items.Items));
        await _api.StoreSyncTokenAsync("rootlist", targetRevision, CancellationToken.None);
    }

    private void HandleAddOp(Add add)
    {
        var fromIndex = add.FromIndex;
        var items = add.Items;

        // Flatten the existing hierarchy
        var entireListFlattenedOut = FlattenHierarchy(_items.Items).OrderBy(x => x.Index).ToList();

        int actualIndex = fromIndex - 1;
        foreach (var toAddItem in add.Items)
        {
            actualIndex++;
            // Now we find the item at this index
            var elementAtIndex = entireListFlattenedOut.FirstOrDefault(x => x.Index == actualIndex);
            var parent = elementAtIndex?.Parent;

            if (toAddItem.Uri.StartsWith("spotify:start-group"))
            {
                var folderName = WebUtility.UrlDecode(toAddItem.Uri.Split(':').Last());
                var x = new SpotifyPlaylistOrFolderForSorting(
                    Uri: toAddItem.Uri,
                    Parent: parent,
                    Item: new SpotifyFolder(
                        id: SpotifyId.FromUri(toAddItem.Uri),
                        name: folderName,
                        index: actualIndex,
                        addedAt: DateTimeOffset.UnixEpoch,
                        folderGroupId: ParseFolderIdFromUri(toAddItem.Uri)
                    ),
                    Index: actualIndex,
                    Type: SpotifyPlaylistOrFolderType.StartGroup
                );
                ((SpotifyFolder)x.Item).StartGroupUri = toAddItem.Uri;

                entireListFlattenedOut.Insert(
                    actualIndex,
                    x
                );
            }
            else if (toAddItem.Uri.StartsWith("spotify:end-group"))
            {
                var folderId = ParseFolderIdFromUri(toAddItem.Uri);
                var folder = FindFolderByGroupId(folderId);
                if (folder is null)
                {
                    _logger.LogWarning("Could not find folder with ID {FolderId}", folderId);
                    continue;
                }

                // finish the folder
                folder.EndGroupUri = toAddItem.Uri;
                folder.EndIndex = actualIndex;
                var x = new SpotifyPlaylistOrFolderForSorting(
                    Uri: toAddItem.Uri,
                    Parent: parent,
                    Item: folder,
                    Index: actualIndex,
                    Type: SpotifyPlaylistOrFolderType.EndGroup
                );
                ((SpotifyFolder)x.Item).EndGroupUri = toAddItem.Uri;
                entireListFlattenedOut.Insert(
                    actualIndex,
                    x);
            }
            else
            {
                //TODO:
            }
        }


        // now we update the indices of the items
        // if the parent is null, it means it's a top-level item, so we do _addOrUpdate
        // otherwise, we do _update
        actualIndex = -1;

        _items.Edit(f =>
        {
            foreach (var (uri, parent, item, index, type) in entireListFlattenedOut.Select((x, i) =>
                         (x.Uri, x.Parent, x.Item, i, x.Type)))
            {
                actualIndex++;
                // It is possible that we moved an item into a new folder
                // in that case, it should be between the start and end group
                if (type is SpotifyPlaylistOrFolderType.StartGroup)
                {
                    // Update the index of the folder
                    item.Index = actualIndex;
                    if (parent is null)
                    {
                        f.AddOrUpdate(item);
                    }
                    else
                    {
                        parent.UpdateItem(item);
                    }
                }
                else if (type is SpotifyPlaylistOrFolderType.EndGroup)
                {
                    var folderId = ParseFolderIdFromUri(uri);
                    var folder = FindFolderByGroupId(folderId);
                    if (folder is null)
                    {
                        _logger.LogWarning("Could not find folder with ID {FolderId}", folderId);
                        continue;
                    }

                    folder.EndIndex = actualIndex;
                    if (parent is null)
                    {
                        f.AddOrUpdate(folder);
                    }
                    else
                    {
                        parent.UpdateItem(folder);
                    }
                }
                else
                {
                    var activeFolder = parent as SpotifyFolder;
                    // now it is also possible, that the item was previously not in a folder
                    // and now it is, so we need to add it to the folder
                    if (item is SpotifyPlaylist p)
                    {
                        p.Index = actualIndex;
                        if (activeFolder is not null)
                        {
                            if (!string.IsNullOrEmpty(activeFolder?.EndGroupUri))
                            {
                                // remove
                                activeFolder.Add(p);
                            }
                        }
                        else
                        {
                            f.AddOrUpdate(p);
                        }
                    }
                }
            }
        });
    }

    // Method to find a folder by FolderGroupId in the hierarchy
    private SpotifyFolder? FindFolderByGroupId(string groupId)
    {
        foreach (var item in _items.Items)
        {
            if (item is SpotifyFolder folder)
            {
                var result = folder.FindFolderByGroupId(groupId);
                if (result != null)
                    return result;
            }
        }

        return null;
    }


    private void HandleRemoveOp(Rem rem)
    {
        var fromIndex = rem.FromIndex;
        var length = rem.Length;

        // Flatten the existing hierarchy and sort by index
        var entireListFlattenedOut = FlattenHierarchy(_items.Items)
            .OrderBy(x => x.Index)
            .ToList();

        // Remove the range of items from the list
        var removedItems = entireListFlattenedOut.GetRange(fromIndex, length);
        foreach (var item in removedItems)
        {
            if (item.Type is SpotifyPlaylistOrFolderType.StartGroup)
            {
                item.Index = -1;
                item.Item.Index = -1;
            }
            else if (item.Type is SpotifyPlaylistOrFolderType.EndGroup)
            {
                ((SpotifyFolder)item.Item).EndIndex = -1;
            }
            else
            {
                item.Item.Index = -1;
                item.Index = -1;
            }
        }

        // Now we update the indices of the remaining items and rebuild the hierarchy
        int actualIndex = 0;
        Stack<SpotifyFolder> stack = new();

        _items.Edit(f =>
        {
            foreach (var (uri, parent, item, index, type) in entireListFlattenedOut.Select((x, i) =>
                         (x.Uri, x.Parent, x.Item, i, x.Type)))
            {
                if (type == SpotifyPlaylistOrFolderType.StartGroup)
                {
                    var folder = (SpotifyFolder)item;
                    if (item.Index is not -1)
                    {
                        folder.Index = actualIndex;
                        actualIndex++;
                        if (parent is null)
                        {
                            f.AddOrUpdate(folder);
                        }
                        else
                        {
                            parent.UpdateItem(folder);
                        }
                    }
                    else
                    {
                        if (folder.Index == -1 && folder.EndIndex == -1)
                        {
                            // we are removing an item where the start and end group are removed
                            // so we need to remove the folder
                            folder.Dispose();
                            if (parent is null)
                            {
                                f.Remove(folder);
                            }
                            else
                            {
                                parent.Remove(folder);
                            }
                        }
                        else
                        {
                            // update for now
                            if (parent is null)
                            {
                                f.AddOrUpdate(folder);
                            }
                            else
                            {
                                parent.UpdateItem(folder);
                            }
                        }
                    }
                }
                else if (type == SpotifyPlaylistOrFolderType.EndGroup)
                {
                    var folder = (SpotifyFolder)item;
                    if (folder.EndIndex is not -1)
                    {
                        folder.EndIndex = actualIndex;
                        actualIndex++;
                        if (parent is null)
                        {
                            f.AddOrUpdate(folder);
                        }
                        else
                        {
                            parent.UpdateItem(folder);
                        }
                    }
                    else
                    {
                        if (folder.Index == -1 && folder.EndIndex == -1)
                        {
                            // we are removing an item where the start and end group are removed
                            // so we need to remove the folder
                            if (parent is null)
                            {
                                f.Remove(folder);
                            }
                            else
                            {
                                parent.Remove(folder);
                            }
                        }
                        else
                        {
                            // update for now
                            if (parent is null)
                            {
                                f.AddOrUpdate(folder);
                            }
                            else
                            {
                                parent.UpdateItem(folder);
                            }
                        }
                    }
                }
                else // Playlist
                {
                    if (item.Index is not -1)
                    {
                        item.Index = actualIndex;
                        actualIndex++;
                        if (parent is null)
                        {
                            f.AddOrUpdate(item);
                        }
                        else
                        {
                            parent.UpdateItem(item);
                        }
                    }
                    else
                    {
                        if (item.Index == -1)
                        {
                            if (parent is null)
                            {
                                f.Remove(item);
                            }
                            else
                            {
                                parent.Remove(item);
                            }
                        }
                    }
                }
            }
        });
    }

    private void HandleMoveOp(Mov op)
    {
        var fromIndex = op.FromIndex;
        var length = op.Length;
        var toIndex = op.ToIndex;

        // First we flatten the entire list, and sort it by index
        // This is assuming all items are in the correct order
        var entireListFlattenedOut = FlattenHierarchy(_items.Items).OrderBy(x => x.Index).ToList();

        // Remove the range of items from the list
        var removedItems = entireListFlattenedOut.GetRange(fromIndex, length);
        entireListFlattenedOut.RemoveRange(fromIndex, length);

        // now insert the items at the new index, but we need to adjust the index of the items
        // that are moved to the new index
        var adjustedInsertionIndex = toIndex;
        entireListFlattenedOut.InsertRange(adjustedInsertionIndex, removedItems);

        // now we update the indices of the items
        // if the parent is null, it means it's a top-level item, so we do _addOrUpdate
        // otherwise, we do _update
        int actualIndex = -1;

        Stack<SpotifyFolder> stack = new();
        _items.Edit(f =>
        {
            foreach (var (parent, item, index, type) in entireListFlattenedOut.Select((x, i) =>
                         (x.Parent, x.Item, i, x.Type)))
            {
                actualIndex++;
                // It is possible that we moved an item into a new folder
                // in that case, it should be between the start and end group
                if (type is SpotifyPlaylistOrFolderType.StartGroup)
                {
                    stack.Push((SpotifyFolder)item);

                    // Update the index of the folder
                    item.Index = actualIndex;
                    if (parent is null)
                    {
                        f.AddOrUpdate(item);
                    }
                    else
                    {
                        parent.UpdateItem(item);
                    }
                }
                else if (type is SpotifyPlaylistOrFolderType.EndGroup)
                {
                    var folder = stack.Pop();
                    folder.EndIndex = actualIndex;
                    if (parent is null)
                    {
                        f.AddOrUpdate(folder);
                    }
                    else
                    {
                        parent.UpdateItem(folder);
                    }
                }
                else
                {
                    var activeFolder = stack.Count > 0 ? stack.Peek() : null;
                    // now it is possible that the item was previously in a folder
                    // and now it is not, so we need to remove it from the folder
                    if (item is SpotifyPlaylist playlist)
                    {
                        if (parent is { } folder)
                        {
                            // check if the playlist is in the folder
                            if (activeFolder?.Id != folder.Id)
                            {
                                folder.Remove(playlist);
                            }
                        }
                    }

                    // now it is also possible, that the item was previously not in a folder
                    // and now it is, so we need to add it to the folder
                    if (item is SpotifyPlaylist p)
                    {
                        p.Index = actualIndex;
                        if (activeFolder is not null)
                        {
                            // remove
                            f.Remove(p);
                            activeFolder.Add(p);
                        }
                        else
                        {
                            f.AddOrUpdate(p);
                        }
                    }
                }
            }
        });
    }

    private enum SpotifyPlaylistOrFolderType
    {
        Playlist,
        StartGroup,
        EndGroup
    }

    private record SpotifyPlaylistOrFolderForSorting(
        string Uri,
        SpotifyFolder? Parent,
        SpotifyPlaylistOrFolder Item,
        int Index,
        SpotifyPlaylistOrFolderType Type)
    {
        public int Index { get; set; } = Index;
    }

    private List<SpotifyPlaylistOrFolderForSorting> FlattenHierarchy(
        IReadOnlyList<SpotifyPlaylistOrFolder> itemsItems,
        SpotifyFolder? activeFolder = null)
    {
        var output = new List<SpotifyPlaylistOrFolderForSorting>();
        foreach (var item in itemsItems)
        {
            if (item is SpotifyPlaylist playlist)
            {
                output.Add(new SpotifyPlaylistOrFolderForSorting(
                    playlist.Id.ToString(),
                    activeFolder, playlist, playlist.Index,
                    SpotifyPlaylistOrFolderType.Playlist));
            }
            else if (item is SpotifyFolder folder)
            {
                var startGroupIndex = folder.Index;
                if (startGroupIndex is not -1)
                {
                    output.Add(new SpotifyPlaylistOrFolderForSorting(
                        folder.StartGroupUri,
                        activeFolder, folder, startGroupIndex,
                        SpotifyPlaylistOrFolderType.StartGroup));
                }

                output.AddRange(FlattenHierarchy(folder.Items, folder));

                if (!string.IsNullOrEmpty(folder.EndGroupUri) && folder.EndIndex is not -1)
                {
                    var endGroupIndex = folder.EndIndex;
                    output.Add(new SpotifyPlaylistOrFolderForSorting(
                        folder.EndGroupUri ?? folder.StartGroupUri,
                        activeFolder, folder, endGroupIndex,
                        SpotifyPlaylistOrFolderType.EndGroup));
                }
            }
        }

        return output;
    }

    private List<SpotifyPlaylistOrFolder> CreateStructure(IList<SpotifyCachedPlaylistItem> initialPlaylists)
    {
        var items = new List<SpotifyPlaylistOrFolder>();
        var stack = new Stack<SpotifyFolder>();

        // Ensure the items are sorted by Index to maintain the correct order
        var sortedPlaylists = initialPlaylists.OrderBy(p => p.Index);

        foreach (var item in sortedPlaylists)
        {
            var uri = item.Uri;

            if (uri.StartsWith("spotify:start-group:"))
            {
                // Start of a new folder
                var folderName = item.Name;
                var folderId = ParseFolderIdFromUri(item.Uri);
                var folder = new SpotifyFolder(
                    id: SpotifyId.FromUri(uri),
                    name: folderName,
                    index: item.Index,
                    addedAt: item.AddedAt,
                    folderGroupId: folderId
                );

                folder.StartGroupUri = uri;

                if (stack.Count > 0)
                {
                    // Add the folder to the current folder
                    stack.Peek().Add(folder);
                }
                else
                {
                    // Add to the top-level items list
                    items.Add(folder);
                }

                // Push the folder onto the stack
                stack.Push(folder);
            }
            else if (uri.StartsWith("spotify:end-group:"))
            {
                // End of the current folder
                if (stack.Count == 0)
                {
                    _logger.LogWarning("Encountered folder end marker without a matching start: {Uri}", uri);
                    continue; // Or handle error as needed
                }

                var folder = stack.Pop();
                folder.EndGroupUri = uri;
                folder.EndIndex = item.Index;
            }
            else if (uri.StartsWith("spotify:playlist:"))
            {
                // A playlist item
                var playlist = CreateFrom(item);

                if (stack.Count > 0)
                {
                    // Add to the current folder
                    stack.Peek().Add(playlist);
                }
                else
                {
                    // Add to the top-level items list
                    items.Add(playlist);
                }
            }
            else
            {
                _logger.LogWarning("Unknown URI type encountered: {Uri}", uri);
            }
        }

        return items;
    }


    private IList<SpotifyCachedPlaylistItem> CreateCachedPlaylistItems(
        IReadOnlyList<SpotifyPlaylistOrFolder> itemsItems)
    {
        var output = new List<SpotifyCachedPlaylistItem>();

        static void ProcessFolder(SpotifyFolder folder, List<SpotifyCachedPlaylistItem> output)
        {
            // add start-group
            output.Add(new SpotifyCachedPlaylistItem
            {
                Uri = folder.StartGroupUri,
                Name = folder.Name,
                Description = string.Empty,
                Index = folder.Index,
                AddedAt = folder.AddedAt,
                RevisionId = string.Empty,
                Tracks = null
            });

            foreach (var item in folder.Items)
            {
                if (item is SpotifyPlaylist playlist)
                {
                    output.Add(CreatePlaylistItem(playlist));
                }
                else if (item is SpotifyFolder subFolder)
                {
                    ProcessFolder(subFolder, output);
                }
            }

            // add end-group
            output.Add(new SpotifyCachedPlaylistItem
            {
                Uri = folder.EndGroupUri,
                Name = folder.Name,
                Description = string.Empty,
                Index = folder.EndIndex,
                AddedAt = folder.AddedAt,
                RevisionId = string.Empty,
                Tracks = null
            });
        }

        foreach (var item in itemsItems)
        {
            if (item is SpotifyPlaylist playlist)
            {
                output.Add(CreatePlaylistItem(playlist));
            }
            else if (item is SpotifyFolder folder)
            {
                // start-group
                ProcessFolder(folder, output);
            }
        }

        return output;
    }

    public static SpotifyCachedPlaylistItem CreatePlaylistItem(SpotifyPlaylist playlist)
    {
        return new SpotifyCachedPlaylistItem
        {
            Uri = playlist.Id.ToString(),
            Name = playlist.Name,
            Description = string.Empty,
            Index = playlist.Index,
            AddedAt = playlist.AddedAt,
            RevisionId = playlist.Revision,
            Tracks = playlist.Tracks.Select(x => new SpotifyCachedPlaylistTrack
            {
                Index = x.Index,
                Uri = x.Id.ToString(),
                Metadata = x.CreateMetadataDictionary(),
                Initialized = x.Initialized
            }).ToList()
        };
    }

    private List<SpotifyPlaylistOrFolder> CreateStructure(SelectedListContent content)
    {
        var items = new List<SpotifyPlaylistOrFolder>();
        var stack = new Stack<SpotifyFolder>();
        int currentIndex = 0; // Initialize index counter

        foreach (var (item, metaItem) in content.Contents.Items.Zip(content.Contents.MetaItems))
        {
            var uri = item.Uri;

            if (uri.StartsWith("spotify:start-group:"))
            {
                // Start of a new folder
                var folderId = ParseFolderIdFromUri(uri);
                var folderName = WebUtility.UrlDecode(uri.Split(':').Last());

                var idx = currentIndex;

                var folderItem = new SpotifyFolder(
                    id: SpotifyId.FromUri(uri),
                    name: folderName,
                    index: idx,
                    DateTimeOffset.FromUnixTimeMilliseconds(metaItem.Timestamp),
                    folderGroupId: folderId
                );
                folderItem.StartGroupUri = uri;
                currentIndex++;
                if (stack.Count > 0)
                {
                    stack.Peek().Add(folderItem);
                }
                else
                {
                    items.Add(folderItem);
                }

                stack.Push(folderItem);
                _logger.LogInformation("Added Folder: {FolderName} (ID: {FolderId}) at Index: {Index}", folderName,
                    folderId, folderItem.Index);
            }
            else if (uri.StartsWith("spotify:end-group:"))
            {
                // End of the current folder
                if (stack.Count == 0)
                {
                    _logger.LogWarning($"Encountered folder end marker without a matching start: {item.Uri}");
                    continue; // Or handle error as needed
                }

                var finalFolder = stack.Pop();
                finalFolder.EndGroupUri = uri;
                finalFolder.EndIndex = currentIndex;
                currentIndex++;
            }
            else if (uri.StartsWith("spotify:playlist:"))
            {
                // A playlist item
                var playlistId = SpotifyId.FromUri(uri);
                var playlistName = WebUtility.UrlDecode(metaItem.Attributes.Name);
                var playlistDescription = metaItem.Attributes.Description ?? string.Empty;

                var idx = currentIndex;
                var tracks = new List<SpotifyPlaylistTrack>(metaItem.Length);
                for (int i = 0; i < metaItem.Length; i++)
                {
                    var pseudoTrack = new SpotifyPlaylistTrack();
                    pseudoTrack.Index = i;
                    pseudoTrack.OriginalIndex = i;
                    tracks.Add(pseudoTrack);
                }

                var playlist = new SpotifyPlaylist(
                    id: playlistId,
                    name: playlistName,
                    index: idx,
                    addedAt: DateTimeOffset.FromUnixTimeMilliseconds(metaItem.Timestamp),
                    revision: metaItem.Revision.ToBase64(),
                    _state,
                    tracks,
                    _api,
                    _playlistRepository,
                    _logger
                );


                currentIndex++;
                if (stack.Count > 0)
                {
                    stack.Peek().Add(playlist); // Assuming AddFolder can accept playlists
                }
                else
                {
                    items.Add(playlist);
                }

                _logger.LogInformation("Added Playlist: {PlaylistName} (ID: {PlaylistId}) at Index: {Index}",
                    playlistName, playlistId, playlist.Index);
            }
            else
            {
                _logger.LogWarning("Unknown URI type encountered: {Uri}", uri);
            }
        }

        return items;
    }

    /// <summary>
    /// Extracts the folder ID from a folder URI.
    /// </summary>
    /// <param name="uri">The folder URI.</param>
    /// <returns>The extracted folder ID.</returns>
    private string ParseFolderIdFromUri(string uri)
    {
        // Example URI: "spotify:start-group:folderId"
        var parts = uri.Split(':');
        if (parts.Length >= 3)
        {
            return parts[2];
        }
        else
        {
            _logger.LogWarning("Unable to parse folder URI: {Uri}", uri);
            return "unknown_folder_id";
        }
    }

    private SpotifyPlaylist CreateFrom(SpotifyCachedPlaylistItem item)
    {
        var uri = item.Uri;
        var playlistId = SpotifyId.FromUri(uri);
        var playlistName = item.Name;
        var playlistRevision = item.RevisionId;
        var tracks = new List<SpotifyPlaylistTrack>(item.Tracks.Count);
        foreach (var track in item.Tracks)
        {
            SpotifyId id = default;
            if (!string.IsNullOrEmpty(track.Uri))
            {
                id = SpotifyId.FromUri(track.Uri);
            }

            var metadata = track.Metadata;
            var index = track.Index;
            var addedBy = metadata.GetValueOrDefault("added_by", string.Empty);
            var addedAt = metadata.TryGetValue("added_at", out var addedAtString)
                ? DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(addedAtString))
                : (DateTimeOffset?)null;
            string? uid = null;
            if (metadata.TryGetValue("uid", out var uidString))
            {
                uid = uidString;
            }

            var playlistTrack = new SpotifyPlaylistTrack();
            playlistTrack.Id = id;
            playlistTrack.Uid = uid;
            playlistTrack.Index = index;
            playlistTrack.OriginalIndex = index;
            playlistTrack.AddedAt = addedAt;
            playlistTrack.AddedBy = addedBy;
            playlistTrack.Initialized = track.Initialized;
            tracks.Add(playlistTrack);
        }

        var playlist = new SpotifyPlaylist(
            id: playlistId,
            name: playlistName,
            index: item.Index,
            addedAt: item.AddedAt,
            revision: playlistRevision,
            spotifyWebsocketState: _state,
            tracks,
            _api,
            _playlistRepository,
            _logger
        );
        
        return playlist;
    }

    public void Dispose()
    {
    }
}