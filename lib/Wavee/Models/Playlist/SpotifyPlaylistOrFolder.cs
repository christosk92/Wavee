using System.ComponentModel;
using System.Runtime.CompilerServices;
using Wavee.Models.Common;

namespace Wavee.Models.Playlist;

/// <summary>
/// Represents a base class for a Spotify playlist or folder within the Spotify playlist hierarchy.
/// </summary>
/// <remarks>
/// The <see cref="SpotifyPlaylistOrFolder"/> class serves as an abstract base for different types of items that can exist in a Spotify playlist structure, specifically playlists and folders.
/// <para>
/// Implementations of this class include:
/// </para>
/// <list type="bullet">
///   <item>
///     <description><see cref="SpotifyPlaylist"/>: Represents a Spotify playlist containing tracks.</description>
///   </item>
///   <item>
///     <description><see cref="SpotifyFolder"/>: Represents a folder that can contain playlists and other folders.</description>
///   </item>
/// </list>
/// <para>
/// **Usage Example**:
/// </para>
/// <code>
/// // Creating a playlist
/// var playlist = new SpotifyPlaylist(
///     id: new SpotifyId("playlist_id"),
///     name: "My Playlist",
///     index: 0,
///     addedAt: DateTimeOffset.UtcNow,
///     revision: "revision_id",
///     spotifyWebsocketState: spotifyWebsocketState,
///     tracks: new List&lt;SpotifyPlaylistTrack&gt;(),
///     spotifyApiClient: spotifyApiClient,
///     spotifyPlaylistRepository: spotifyPlaylistRepository,
///     logger: logger);
///
/// // Creating a folder
/// var folder = new SpotifyFolder(
///     id: new SpotifyId("folder_id"),
///     name: "My Folder",
///     index: 1,
///     addedAt: DateTimeOffset.UtcNow,
///     folderGroupId: "group_id");
///
/// // Adding items to the folder
/// folder.Add(playlist);
///
/// // Accessing items
/// foreach (var item in folder.Items)
/// {
///     Console.WriteLine($"Item Name: {item.Name}, Type: {item.GetType().Name}");
/// }
/// </code>
/// <para>
/// **Threading Considerations**:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       Instances of <see cref="SpotifyPlaylistOrFolder"/> and its derived classes are not thread-safe. Synchronization is required if instances are accessed from multiple threads.
///     </description>
///   </item>
/// </list>
/// </remarks>
public abstract class SpotifyPlaylistOrFolder : INotifyPropertyChanged, IDisposable
{
    private int _index;
    private string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpotifyPlaylistOrFolder"/> class.
    /// </summary>
    /// <param name="id">The unique Spotify ID of the playlist or folder.</param>
    /// <param name="name">The name of the playlist or folder.</param>
    /// <param name="index">The index position of the playlist or folder in the playlist hierarchy.</param>
    /// <param name="addedAt">The timestamp when the playlist or folder was added.</param>
    protected SpotifyPlaylistOrFolder(SpotifyId id, string name, int index, DateTimeOffset addedAt)
    {
        Id = id;
        Name = name;
        Index = index;
        AddedAt = addedAt;
    }

    /// <summary>
    /// Gets the unique Spotify ID identifying the playlist or folder.
    /// </summary>
    public SpotifyId Id { get; }

    /// <summary>
    /// Gets the timestamp indicating when the playlist or folder was added.
    /// </summary>
    public DateTimeOffset AddedAt { get; }

    /// <summary>
    /// Gets or sets the name of the playlist or folder.
    /// </summary>
    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    /// <summary>
    /// Gets or sets the index position of the playlist or folder in the playlist hierarchy.
    /// </summary>
    /// <remarks>
    /// The index represents the position of the item within the parent folder or root playlist collection.
    /// </remarks>
    public int Index
    {
        get => _index;
        set => SetField(ref _index, value);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="SpotifyPlaylistOrFolder"/> class and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Release managed resources here, if necessary.
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="SpotifyPlaylistOrFolder"/>.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event to notify UI elements of property value changes.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Sets the field to a new value and raises the <see cref="PropertyChanged"/> event if the value has changed.
    /// </summary>
    /// <typeparam name="T">The type of the field being set.</typeparam>
    /// <param name="field">A reference to the field to set.</param>
    /// <param name="value">The new value to assign to the field.</param>
    /// <param name="propertyName">The name of the property that changed.</param>
    /// <returns><see langword="true"/> if the field value changed; otherwise, <see langword="false"/>.</returns>
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}