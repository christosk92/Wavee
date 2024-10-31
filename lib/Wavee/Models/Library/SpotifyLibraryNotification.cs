namespace Wavee.Models.Library;

/// <summary>
/// Represents a notification about a change to a Spotify library item.
/// </summary>
/// <remarks>
/// The <see cref="SpotifyLibraryNotification"/> class encapsulates information about a change to a library item, including the item itself and the type of action that occurred.
/// <para>
/// **Usage Example**:
/// </para>
/// <code>
/// // Handling a library notification
/// void HandleNotification(SpotifyLibraryNotification notification)
/// {
///     switch (notification.Action)
///     {
///         case SpotifyLibraryNotificationType.Added:
///             Console.WriteLine($"Item added: {notification.Item.Id}");
///             break;
///         case SpotifyLibraryNotificationType.Removed:
///             Console.WriteLine($"Item removed: {notification.Item.Id}");
///             break;
///         case SpotifyLibraryNotificationType.Updated:
///             Console.WriteLine($"Item updated: {notification.Item.Id}");
///             break;
///     }
/// }
/// </code>
/// </remarks>
/// <param name="Item">The <see cref="SpotifyLibraryItem"/> that was affected.</param>
/// <param name="Action">The type of action that occurred.</param>
public record SpotifyLibraryNotification(SpotifyLibraryItem Item, SpotifyLibraryNotificationType Action);