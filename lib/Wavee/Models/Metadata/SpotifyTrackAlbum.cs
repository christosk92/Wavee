using Wavee.Models.Common;

namespace Wavee.Models.Metadata;

/// <summary>
/// Represents an album of a Spotify track.
/// </summary>
public sealed class SpotifyTrackAlbum : SpotifyItem
{
    /// <summary>
    /// Gets or sets the type of the Spotify track album.
    /// </summary>
    public SpotifyTrackAlbumType Type { get; set; }

    /// <summary>
    /// Gets or sets the collection of images associated with the Spotify track album.
    /// The key represents the size of the image, while the value holds the corresponding image details.
    /// </summary>
    public Dictionary<SpotifyImageSize, SpotifyImage> Images { get; set; }
}