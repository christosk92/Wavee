namespace Wavee.Models.Library;

/// <summary>
/// Specifies the type of action that occurred to a Spotify library item.
/// </summary>
/// <remarks>
/// The <see cref="SpotifyLibraryNotificationType"/> enumeration defines the possible actions that can occur to a library item, such as addition, removal, or update.
/// <para>
/// **Possible Values**:
/// </para>
/// <list type="bullet">
///   <item>
///     <description><see cref="Added"/>: The item was added to the library.</description>
///   </item>
///   <item>
///     <description><see cref="Removed"/>: The item was removed from the library.</description>
///   </item>
///   <item>
///     <description><see cref="Updated"/>: The item was updated in the library.</description>
///   </item>
/// </list>
/// </remarks>
public enum SpotifyLibraryNotificationType
{
    /// <summary>
    /// Indicates that the item was added to the library.
    /// </summary>
    Added,

    /// <summary>
    /// Indicates that the item was removed from the library.
    /// </summary>
    Removed,

    /// <summary>
    /// Indicates that the item was updated in the library.
    /// </summary>
    Updated
}