namespace Wavee.Models.Metadata;

/// <summary>
/// Represents the type of a Spotify album.
/// </summary>
public enum SpotifyTrackAlbumType
{
    /// <summary>
    /// Represents a full studio album.
    /// </summary>
    Album,

    /// <summary>
    /// Represents a single release.
    /// </summary>
    Single,

    /// <summary>
    /// Represents a compilation album which typically features tracks from various artists or previously released tracks.
    /// </summary>
    Compilation,

    /// <summary>
    /// Represents an extended play (EP) album, typically containing more tracks than a single but fewer than a full album.
    /// </summary>
    EP,

    /// <summary>
    /// Represents an audiobook.
    /// </summary>
    Audiobook,

    /// <summary>
    /// Represents a podcast.
    /// </summary
    Podcast
}