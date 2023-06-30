namespace Wavee.Id;

/// <summary>
/// An enum representing the type of an audio item.
/// </summary>
[Flags]
public enum AudioItemType
{
    /// <summary>
    /// The audio item is a track.
    /// </summary>
    Track = 0,
    /// <summary>
    /// The audio item is an album.
    /// </summary>
    Album = 1,
    /// <summary>
    /// The audio item is an artist.
    /// </summary>
    Artist = 2,
    /// <summary>
    /// The audio item is a playlist.
    /// </summary>
    Playlist = 4,
    
    /// <summary>
    /// The audio item is an episode from a podcast.
    /// </summary>
    PodcastEpisode = 8,
    Unknown = 16,
    PodcastShow = 32,
    UserCollection = 64,
    Prerelease = 128,
    Concert = 256,
}