using System.Collections.ObjectModel;
using Wavee.Models.Common;
using Wavee.Models.Playlist;

namespace Wavee.Interfaces
{
    /// <summary>
    /// Provides functionality for managing and interacting with Spotify playlists, including initialization, retrieval, and real-time updates.
    /// </summary>
    /// <remarks>
    /// The <see cref="ISpotifyPlaylistClient"/> interface defines methods and properties for accessing and managing the user's Spotify playlists and folders.
    /// It allows for initializing the playlist client, accessing a collection of playlists, and monitoring the initialization status.

    /// <para>
    /// **Implementations**:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       Implementations of this interface should handle the retrieval and caching of playlists, as well as real-time updates from Spotify.
    ///     </description>
    ///   </item>
    /// </list>

    /// <para>
    /// **Usage Example**:
    /// </para>
    /// <code>
    /// // Create an instance of ISpotifyPlaylistClient (assuming a concrete implementation is available)
    /// ISpotifyPlaylistClient playlistClient = new SpotifyPlaylistClient(spotifyApiClient, logger);
    ///
    /// // Initialize the client
    /// await playlistClient.Initialize();
    ///
    /// // Subscribe to initialization status
    /// var initSubscription = playlistClient.Initialized.Subscribe(isInitialized =>
    /// {
    ///     if (isInitialized)
    ///     {
    ///         Console.WriteLine("Playlist client initialized.");
    ///     }
    /// });
    ///
    /// // Access the playlists
    /// foreach (var item in playlistClient.Playlists)
    /// {
    ///     if (item is SpotifyPlaylist playlist)
    ///     {
    ///         Console.WriteLine($"Playlist: {playlist.Name}");
    ///     }
    ///     else if (item is SpotifyFolder folder)
    ///     {
    ///         Console.WriteLine($"Folder: {folder.Name}");
    ///     }
    /// }
    ///
    /// // Remember to dispose of subscriptions when done
    /// initSubscription.Dispose();
    /// </code>

    /// <para>
    /// **Threading Considerations**:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       The playlist collection and initialization status observable may raise events on background threads. Consumers should handle threading appropriately, such as dispatching to the UI thread if necessary.
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    public interface ISpotifyPlaylistClient
    {
        /// <summary>
        /// Initializes the Spotify playlist client by fetching all existing playlists the user has.
        /// This method must be called before processing any add or remove operations.
        /// During initialization, no items can be added or removed from the cache.
        /// </summary>
        /// <remarks>
        /// The <see cref="Initialize"/> method performs an asynchronous operation to fetch the user's playlists from Spotify. It must be called and completed successfully before the client can be used to interact with playlists.

        /// <para>
        /// **Usage Example**:
        /// </para>
        /// <code>
        /// // Initialize the playlist client
        /// await playlistClient.Initialize();
        /// </code>

        /// <para>
        /// **Threading Considerations**:
        /// </para>
        /// <list type="bullet">
        ///   <item>
        ///     <description>
        ///       The method is asynchronous and should be awaited to ensure initialization completes before proceeding.
        ///     </description>
        ///   </item>
        /// </list>
        /// </remarks>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous initialization operation.
        /// The task completes when all existing playlists have been fetched and added to the cache.
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown if an error occurs while fetching the initial playlists.
        /// </exception>
        Task Initialize(CancellationToken cancellationToken = default);

        /// <summary>
        /// Provides a read-only observable collection of all Spotify playlists and folders, updated in real-time.
        /// </summary>
        /// <remarks>
        /// The <see cref="Playlists"/> property exposes a <see cref="ReadOnlyObservableCollection{SpotifyPlaylistOrFolder}"/> that contains the user's Spotify playlists and folders.
        /// This collection is updated in real-time to reflect any changes, such as additions, removals, or modifications to playlists or folders.

        /// <para>
        /// **Usage Example**:
        /// </para>
        /// <code>
        /// // Accessing playlists after initialization
        /// foreach (var item in playlistClient.Playlists)
        /// {
        ///     if (item is SpotifyPlaylist playlist)
        ///     {
        ///         Console.WriteLine($"Playlist: {playlist.Name}");
        ///     }
        ///     else if (item is SpotifyFolder folder)
        ///     {
        ///         Console.WriteLine($"Folder: {folder.Name}");
        ///     }
        /// }
        /// </code>

        /// <para>
        /// **Threading Considerations**:
        /// </para>
        /// <list type="bullet">
        ///   <item>
        ///     <description>
        ///       The collection may raise collection changed events on background threads. Consumers should handle threading appropriately, such as dispatching to the UI thread if necessary.
        ///     </description>
        ///   </item>
        /// </list>
        /// </remarks>
        /// <value>
        /// A <see cref="ReadOnlyObservableCollection{SpotifyPlaylistOrFolder}"/> containing the user's playlists and folders, updated in real-time.
        /// </value>
        ReadOnlyObservableCollection<SpotifyPlaylistOrFolder> Playlists { get; }

        /// <summary>
        /// Indicates whether the Spotify Playlists client has completed initialization.
        /// Consumers can subscribe to this observable to be notified when initialization is finished.
        /// </summary>
        /// <remarks>
        /// The <see cref="Initialized"/> property provides an <see cref="IObservable{Boolean}"/> that emits values indicating the initialization status of the playlist client.

        /// <para>
        /// **Emission Behavior**:
        /// </para>
        /// <list type="number">
        ///   <item>
        ///     <description>
        ///       The observable emits a <c>false</c> value when subscription occurs if the client is not yet initialized.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       Once initialization completes successfully, the observable emits a <c>true</c> value.
        ///     </description>
        ///   </item>
        /// </list>

        /// <para>
        /// **Usage Example**:
        /// </para>
        /// <code>
        /// // Subscribe to initialization status
        /// var initSubscription = playlistClient.Initialized.Subscribe(isInitialized =>
        /// {
        ///     if (isInitialized)
        ///     {
        ///         Console.WriteLine("Playlist client initialized.");
        ///     }
        ///     else
        ///     {
        ///         Console.WriteLine("Playlist client not yet initialized.");
        ///     }
        /// });
        /// </code>

        /// <para>
        /// **Threading Considerations**:
        /// </para>
        /// <list type="bullet">
        ///   <item>
        ///     <description>
        ///       The observable may emit values on background threads. Consumers should handle threading appropriately.
        ///     </description>
        ///   </item>
        /// </list>
        /// </remarks>
        /// <value>
        /// An <see cref="IObservable{Boolean}"/> that emits initialization status updates.
        /// </value>
        IObservable<bool> Initialized { get; }

        /// <summary>
        /// Retrieves a specific playlist by its unique identifier from Spotify.
        /// </summary>
        /// <param name="id">The unique identifier of the playlist to retrieve.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation to fetch the playlist.
        /// Upon success, the task result contains the <see cref="SpotifyPlaylist"/> corresponding to the specified identifier.
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown if an error occurs while fetching the playlist.
        /// </exception>
        Task<SpotifyPlaylist> GetPlaylist(SpotifyId id, CancellationToken cancellationToken);
    }
}
