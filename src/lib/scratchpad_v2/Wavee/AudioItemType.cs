namespace Wavee;

/// <summary>
/// An enum indicating the specific type of audio item.
/// </summary>
public enum AudioItemType
{
    /// <summary>
    /// Wavee does not understand the specific type and will be ignored by everything.
    /// </summary>
    Unknown,
    /// <summary>
    /// A type of track.
    /// </summary>
    Track,
    /// <summary>
    /// A type of artist.
    /// </summary>
    Artist,
    /// <summary>
    /// A type of album.
    /// </summary>
    Album,
    /// <summary>
    /// A type of playlist.
    /// </summary>
    Playlist,
    /// <summary>
    /// A type of user.
    /// </summary>
    User,
    /// <summary>
    /// A type of podcast how.
    /// </summary>
    Show,
    /// <summary>
    /// A type of podcast episode of a sho.
    /// </summary>
    Episode,
    /// <summary>
    /// A type of mixed collection.
    /// </summary>
    Collection,
    /// <summary>
    /// A type of radio station (radio of a track).
    /// </summary>
    Station,
    Local,
    Image
}