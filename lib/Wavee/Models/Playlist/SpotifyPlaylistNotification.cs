namespace Wavee.Models.Playlist;

/// <summary>
/// Represents the type of notification for playlist changes.
/// </summary>
public enum SpotifyPlaylistNotificationType
{
    Added,
    Removed,
    Updated
}

/// <summary>
/// Represents a notification for a playlist change.
/// </summary>
public sealed class SpotifyPlaylistNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SpotifyPlaylistNotification"/> class.
    /// </summary>
    /// <param name="playlist">The playlist item associated with the notification.</param>
    /// <param name="action">The type of action that occurred.</param>
    public SpotifyPlaylistNotification(object playlist, SpotifyPlaylistNotificationType action)
    {
        Playlist = playlist ?? throw new ArgumentNullException(nameof(playlist));
        Action = action;
    }

    /// <summary>
    /// Gets the playlist item associated with the notification.
    /// </summary>
    public object Playlist { get; }

    /// <summary>
    /// Gets the type of action that occurred.
    /// </summary>
    public SpotifyPlaylistNotificationType Action { get; }
}