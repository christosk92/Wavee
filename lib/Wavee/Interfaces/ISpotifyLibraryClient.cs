using System.Collections.ObjectModel;
using DynamicData;
using Wavee.Models.Common;
using Wavee.Models.Library;

namespace Wavee.Interfaces;

/// <summary>
/// Provides functionality for managing and interacting with the user's Spotify library, including initialization, retrieval, and real-time updates.
/// </summary>
/// <remarks>
/// The <see cref="ISpotifyLibraryClient"/> interface defines methods and properties for accessing and managing items in the user's Spotify library, such as saved tracks, albums, and other media.
/// <para>
/// **Implementations**:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       Implementations of this interface should handle the retrieval and caching of library items, as well as real-time updates from Spotify.
///     </description>
///   </item>
/// </list>
/// <para>
/// **Usage Example**:
/// </para>
/// <code>
/// // Initialize the client
/// await libraryClient.Initialize();
///
/// // Subscribe to initialization status
/// var initSubscription = libraryClient.Initialized.Subscribe(isInitialized =>
/// {
///     if (isInitialized)
///     {
///         Console.WriteLine("Library client initialized.");
///     }
/// });
///
/// // Access the library items
/// foreach (var item in libraryClient.LibraryItems)
/// {
///     Console.WriteLine($"Item ID: {item.Id}, Added At: {item.AddedAt}");
/// }
///
/// // Subscribe to library notifications
/// var notificationSubscription = libraryClient.Notifications.Subscribe(notifications =>
/// {
///     foreach (var notification in notifications)
///     {
///         switch (notification.Action)
///         {
///             case SpotifyLibraryNotificationType.Added:
///                 Console.WriteLine($"Item added: {notification.Item.Id}");
///                 break;
///             case SpotifyLibraryNotificationType.Removed:
///                 Console.WriteLine($"Item removed: {notification.Item.Id}");
///                 break;
///             case SpotifyLibraryNotificationType.Updated:
///                 Console.WriteLine($"Item updated: {notification.Item.Id}");
///                 break;
///         }
///     }
/// });
///
/// // Remember to dispose of subscriptions when done
/// initSubscription.Dispose();
/// notificationSubscription.Dispose();
/// </code>
/// <para>
/// **Threading Considerations**:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       The library item collection and observables may raise events on background threads. Consumers should handle threading appropriately, such as dispatching to the UI thread if necessary.
///     </description>
///   </item>
/// </list>
/// </remarks>
public interface ISpotifyLibraryClient
{
    /// <summary>
    /// Initializes the Spotify library client by fetching all existing library items.
    /// This method must be called before processing any add or remove operations.
    /// During initialization, no items can be added or removed from the cache.
    /// </summary>
    /// <remarks>
    /// The <see cref="Initialize"/> method performs an asynchronous operation to fetch the user's library items from Spotify. It must be called and completed successfully before the client can be used to interact with library items.

    /// <para>
    /// **Usage Example**:
    /// </para>
    /// <code>
    /// // Initialize the library client
    /// await libraryClient.Initialize();
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
    /// The task completes when all existing library items have been fetched and added to the cache.
    /// </returns>
    /// <exception cref="Exception">
    /// Thrown if an error occurs while fetching the initial library items.
    /// </exception>
    Task Initialize(CancellationToken cancellationToken = default);

    /// <summary>
    /// Occurs when there are changes to the Spotify library, such as additions or removals of items.
    /// Subscribers receive notifications detailing the nature of each change.
    /// </summary>
    /// <remarks>
    /// The <see cref="Notifications"/> observable emits lists of <see cref="SpotifyLibraryNotification"/> objects whenever there are changes to the library.

    /// <para>
    /// **Emission Behavior**:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       The observable emits a list of notifications each time changes occur, detailing the items added, removed, or updated.
    ///     </description>
    ///   </item>
    /// </list>

    /// <para>
    /// **Usage Example**:
    /// </para>
    /// <code>
    /// // Subscribe to library notifications
    /// var notificationSubscription = libraryClient.Notifications.Subscribe(notifications =>
    /// {
    ///     foreach (var notification in notifications)
    ///     {
    ///         switch (notification.Action)
    ///         {
    ///             case SpotifyLibraryNotificationType.Added:
    ///                 Console.WriteLine($"Item added: {notification.Item.Id}");
    ///                 break;
    ///             case SpotifyLibraryNotificationType.Removed:
    ///                 Console.WriteLine($"Item removed: {notification.Item.Id}");
    ///                 break;
    ///             case SpotifyLibraryNotificationType.Updated:
    ///                 Console.WriteLine($"Item updated: {notification.Item.Id}");
    ///                 break;
    ///         }
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
    /// An <see cref="IObservable{IList{SpotifyLibraryNotification}}"/> that emits lists of library notifications when changes occur.
    /// </value>
    IObservable<IList<SpotifyLibraryNotification>> Notifications { get; }

    /// <summary>
    /// Provides a read-only observable collection of all Spotify library items.
    /// Consumers can subscribe to observe real-time changes or query the current state of the library.
    /// </summary>
    /// <remarks>
    /// The <see cref="LibraryItems"/> property exposes a <see cref="ReadOnlyObservableCollection{SpotifyLibraryItem}"/> containing the user's library items. This collection is updated in real-time to reflect any changes.

    /// <para>
    /// **Usage Example**:
    /// </para>
    /// <code>
    /// // Accessing library items after initialization
    /// foreach (var item in libraryClient.LibraryItems)
    /// {
    ///     Console.WriteLine($"Item ID: {item.Id}, Added At: {item.AddedAt}");
    /// }
    /// </code>

    /// <para>
    /// **Threading Considerations**:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       The collection may raise collection changed events on background threads. Consumers should handle threading appropriately.
    ///     </description>
    ///   </item>
    /// </list>

    /// <para>
    /// **Data Integrity**:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       The collection is read-only to prevent external modification. Use the <see cref="Notifications"/> observable to monitor changes.
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    /// <value>
    /// A <see cref="ReadOnlyObservableCollection{SpotifyLibraryItem}"/> containing the user's library items, updated in real-time.
    /// </value>
    IObservableCache<SpotifyLibraryItem, SpotifyId> LibraryItems { get; }

    /// <summary>
    /// Indicates whether the Spotify library client has completed initialization.
    /// Consumers can subscribe to this observable to be notified when initialization is finished.
    /// </summary>
    /// <remarks>
    /// The <see cref="Initialized"/> property provides an <see cref="IObservable{Boolean}"/> that emits values indicating the initialization status of the library client.

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
    /// var initSubscription = libraryClient.Initialized.Subscribe(isInitialized =>
    /// {
    ///     if (isInitialized)
    ///     {
    ///         Console.WriteLine("Library client initialized.");
    ///     }
    ///     else
    ///     {
    ///         Console.WriteLine("Library client not yet initialized.");
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
}