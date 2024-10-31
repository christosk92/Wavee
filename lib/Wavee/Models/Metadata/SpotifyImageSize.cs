namespace Wavee.Models.Metadata;

/// <summary>
/// Specifies the available sizes for Spotify images.
/// This enumeration is used to indicate the intended size of an image associated with a Spotify item.
/// </summary>
public enum SpotifyImageSize
{
    /// <summary>
    /// Represents a small size for a Spotify image.
    /// This can be used for thumbnail or icon purposes when a reduced image size is needed to save space and bandwidth.
    /// </summary>
    Small = 0,

    /// <summary>
    /// Represents a medium size for a Spotify image.
    /// This is suitable for standard display purposes where a balanced trade-off between image detail and load time is desired.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// Represents a large size for a Spotify image.
    /// This is typically used when a high-resolution and detailed image is preferred, such as for album covers or full-size artwork.
    /// </summary>
    Large = 2,
    
    /// <summary>
    /// Represents the default size for a Spotify image.
    /// </summary>
    Default = 3,
}