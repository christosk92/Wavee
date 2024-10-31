using Wavee.Models.Common;

namespace Wavee.Models.Library;

/// <summary>
/// Represents an item in the user's Spotify library.
/// </summary>
/// <remarks>
/// The <see cref="SpotifyLibraryItem"/> class contains information about a library item, including its Spotify ID, when it was added, and the detailed item information once fetched.
/// <para>
/// **Fetching Item Details**:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       The <see cref="Item"/> property holds detailed information about the library item once it has been successfully fetched.
///     </description>
///   </item>
///   <item>
///     <description>
///       If fetching fails, the <see cref="FetchException"/> property contains the exception that occurred.
///     </description>
///   </item>
/// </list>
/// </remarks>
public sealed class SpotifyLibraryItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SpotifyLibraryItem"/> class.
    /// </summary>
    /// <param name="id">The unique Spotify ID of the library item.</param>
    /// <param name="addedAt">The timestamp when the item was added to the library.</param>
    public SpotifyLibraryItem(SpotifyId id, DateTimeOffset addedAt)
    {
        Id = id;
        AddedAt = addedAt;
    }

    /// <summary>
    /// Gets the unique Spotify ID of the library item.
    /// </summary>
    public SpotifyId Id { get; }

    /// <summary>
    /// Gets the timestamp indicating when the item was added to the library.
    /// </summary>
    public DateTimeOffset AddedAt { get; }

    /// <summary>
    /// Gets the detailed playable item information. This is set after successful fetching.
    /// </summary>
    /// <remarks>
    /// The <see cref="Item"/> property contains detailed information about the library item, such as track or album details, after it has been fetched from Spotify.
    /// <para>
    /// **Access Considerations**:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       The property may be <see langword="null"/> if the item has not yet been fetched or if fetching failed.
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    public SpotifyItem? Item { get; internal set; }

    /// <summary>
    /// Gets the exception that occurred during fetching, if any. This is set if fetching fails.
    /// </summary>
    /// <remarks>
    /// The <see cref="FetchException"/> property contains the exception that was thrown during the attempt to fetch the item details. It can be used for error handling and logging.
    /// <para>
    /// **Access Considerations**:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       The property is <see langword="null"/> if no error occurred during fetching.
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    public Exception? FetchException { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether the item has been marked as deleted.
    /// </summary>
    /// <remarks>
    /// The <see cref="Deleted"/> property is used internally to track whether the item has been removed from the library. It is not intended for external use.
    /// <para>
    /// **Access Considerations**:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       The property is <see langword="true"/> if the item has been deleted; otherwise, <see langword="false"/>.
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    internal bool Deleted { get; set; }
}