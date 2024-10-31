namespace Wavee.Models.Library;

/// <summary>
/// Represents a result of a library fetch operation, containing items and a sync token.
/// </summary>
internal sealed class LibraryResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryResult"/> class.
    /// </summary>
    /// <param name="items">The library items fetched.</param>
    /// <param name="syncToken">The synchronization token.</param>
    public LibraryResult(SpotifyLibraryItem[] items, string? syncToken)
    {
        Items = items;
        SyncToken = syncToken;
    }

    /// <summary>
    /// Gets the library items fetched.
    /// </summary>
    public SpotifyLibraryItem[] Items { get; }

    /// <summary>
    /// Gets the synchronization token for future delta fetches.
    /// </summary>
    public string? SyncToken { get; }
}