using System.Collections.ObjectModel;
using DynamicData;
using DynamicData.Binding;
using Wavee.Models.Common;

namespace Wavee.Models.Playlist;

/// <summary>
/// Represents a Spotify folder containing playlists and/or other folders.
/// </summary>
public sealed class SpotifyFolder : SpotifyPlaylistOrFolder
{
    private int _endIndex;
    private readonly IDisposable _subscription;

    private readonly SourceCache<SpotifyPlaylistOrFolder, SpotifyId> _items =
        new(x => x.Id);

    private readonly ReadOnlyObservableCollection<SpotifyPlaylistOrFolder> _itemsReadOnly;

    /// <summary>
    /// Gets the unique group identifier of the folder.
    /// </summary>
    public string FolderGroupId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpotifyFolder"/> class.
    /// </summary>
    /// <param name="id">The unique Spotify ID of the folder.</param>
    /// <param name="name">The name of the folder.</param>
    /// <param name="index">The starting index of the folder.</param>
    /// <param name="addedAt">The timestamp when the folder was added.</param>
    /// <param name="folderGroupId">The unique group identifier for the folder.</param>
    public SpotifyFolder(SpotifyId id, string name, int index, DateTimeOffset addedAt, string folderGroupId)
        : base(id, name, index, addedAt)
    {
        FolderGroupId = folderGroupId;

        _subscription = _items.Connect()
            .AutoRefresh(x => x.Index)
            .Sort(SortExpressionComparer<SpotifyPlaylistOrFolder>.Ascending(x => x.Index))
            .Bind(out _itemsReadOnly)
            .Subscribe();
    }

    /// <summary>
    /// Gets or sets the URI representing the start of the folder group.
    /// </summary>
    internal string? StartGroupUri { get; set; }

    /// <summary>
    /// Gets or sets the URI representing the end of the folder group.
    /// </summary>
    internal string? EndGroupUri { get; set; }

    /// <summary>
    /// Gets or sets the ending index of the folder.
    /// </summary>
    /// <remarks>
    /// The end index represents the index of the end marker for the folder in the Spotify playlist hierarchy.
    /// </remarks>
    public int EndIndex
    {
        get => _endIndex;
        set => SetField(ref _endIndex, value);
    }

    /// <summary>
    /// Gets a read-only collection of items (playlists or folders) contained within this folder.
    /// </summary>
    public ReadOnlyObservableCollection<SpotifyPlaylistOrFolder> Items => _itemsReadOnly;

    /// <summary>
    /// Adds a new item (playlist or folder) to the folder.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public void Add(SpotifyPlaylistOrFolder item)
    {
        _items.AddOrUpdate(item);
    }

    /// <summary>
    /// Removes an item (playlist or folder) from the folder.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <returns>
    /// <see langword="true"/> if the item was successfully removed; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Remove(SpotifyPlaylistOrFolder item)
    {
        var currentItem = _items.Lookup(item.Id);
        if (currentItem.HasValue)
        {
            _items.Remove(item);
            return true;
        }

        // Recursively search in subfolders
        foreach (var i in _itemsReadOnly)
        {
            if (i is SpotifyFolder folder)
            {
                if (folder.Remove(item))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to find an item by its index within the folder and its subfolders.
    /// </summary>
    /// <param name="number">The index number to search for.</param>
    /// <param name="found">When this method returns, contains the found item, if successful; otherwise, <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> if an item with the specified index was found; otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryFindIndex(int number, out SpotifyPlaylistOrFolder? found)
    {
        if (Index == number)
        {
            found = this;
            return true;
        }

        foreach (var item in _itemsReadOnly)
        {
            if (item is SpotifyPlaylist playlist && playlist.Index == number)
            {
                found = playlist;
                return true;
            }

            if (item is SpotifyFolder folder)
            {
                if (folder.TryFindIndex(number, out found))
                {
                    return true;
                }
            }
        }

        found = null;
        return false;
    }

    /// <summary>
    /// Removes an item by its Spotify ID from the folder and its subfolders.
    /// </summary>
    /// <param name="id">The Spotify ID of the item to remove.</param>
    /// <returns>
    /// <see langword="true"/> if the item was successfully removed; otherwise, <see langword="false"/>.
    /// </returns>
    public bool RemoveById(SpotifyId id)
    {
        var item = _items.Lookup(id);
        if (item.HasValue)
        {
            _items.Remove(item.Value);
            return true;
        }

        foreach (var subFolder in _items.Items.OfType<SpotifyFolder>())
        {
            if (subFolder.RemoveById(id))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Updates the index of a playlist item within the folder.
    /// </summary>
    /// <param name="itemId">The Spotify ID of the playlist item to update.</param>
    /// <param name="index">The new index to assign to the playlist item.</param>
    public void UpdatePlaylistIndex(SpotifyId itemId, int index)
    {
        var item = _items.Lookup(itemId);
        if (item.HasValue)
        {
            item.Value.Index = index;
        }
    }

    /// <summary>
    /// Updates the starting index of the folder.
    /// </summary>
    /// <param name="index">The new starting index of the folder.</param>
    public void UpdateFolderStartIndex(int index)
    {
        Index = index;
    }

    /// <summary>
    /// Updates the ending index of the folder.
    /// </summary>
    /// <param name="index">The new ending index of the folder.</param>
    public void UpdateFolderEndIndex(int index)
    {
        EndIndex = index;
    }

    /// <summary>
    /// Updates an item (playlist or folder) within the folder.
    /// </summary>
    /// <param name="item">The item to update.</param>
    public void UpdateItem(SpotifyPlaylistOrFolder item)
    {
        _items.AddOrUpdate(item);
    }

    /// <summary>
    /// Searches for a subfolder within this folder by its group ID.
    /// </summary>
    /// <param name="groupId">The group ID of the folder to find.</param>
    /// <returns>
    /// The <see cref="SpotifyFolder"/> with the specified group ID, if found; otherwise, <see langword="null"/>.
    /// </returns>
    public SpotifyFolder? FindFolderByGroupId(string groupId)
    {
        if (FolderGroupId == groupId)
        {
            return this;
        }

        foreach (var item in _itemsReadOnly)
        {
            if (item is SpotifyFolder folder)
            {
                var found = folder.FindFolderByGroupId(groupId);
                if (found != null)
                {
                    return found;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Releases all resources used by the <see cref="SpotifyFolder"/>.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.
    /// </param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _items.Dispose();
            _subscription.Dispose();

            foreach (var item in _itemsReadOnly)
            {
                item.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}