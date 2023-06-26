namespace Wavee.Id;

/// <summary>
/// An enum representing the type of an audio item.
/// </summary>
public enum AudioItemType
{
    /// <summary>
    /// The audio item is a track.
    /// </summary>
    Track,
    /// <summary>
    /// The audio item is an album.
    /// </summary>
    Album,
    /// <summary>
    /// The audio item is an artist.
    /// </summary>
    Artist,
    /// <summary>
    /// The audio item is a playlist.
    /// </summary>
    Playlist,
    
    /// <summary>
    /// The audio item is an episode from a podcast.
    /// </summary>
    PodcastEpisode,
    Unknown,
    PodcastShow,
    UserCollection
}