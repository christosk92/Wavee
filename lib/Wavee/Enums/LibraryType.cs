namespace Wavee.Enums;

/// <summary>
/// Represents the types of libraries in Spotify.
/// </summary>
public enum LibraryType
{
    /// <summary>
    /// Represents the user's liked songs library.
    /// </summary>
    LikedSongs,

    /// <summary>
    /// Represents the user's followed artists library.
    /// </summary>
    Artists,

    /// <summary>
    /// Represents the user's albums library.
    /// </summary>
    Albums,

    /// <summary>
    /// Represents an unknown library type.
    /// </summary>
    Unknown,
    Rootlist
}