namespace Wavee.Models.Metadata;

/// <summary>
/// Represents an image associated with a Spotify item, such as an album or track.
/// </summary>
public sealed class SpotifyImage
{
    /// <summary>
    /// Gets or sets the size of the Spotify image.
    /// This property is used to indicate the intended size of the image,
    /// such as Small, Medium, or Large, as defined by the <see cref="SpotifyImageSize"/> enumeration.
    /// </summary>
    public required SpotifyImageSize Size { get; set; }

    public required int? Width { get; set; }

    /// <summary>
    /// Gets or sets the URL of the Spotify image.
    /// This property holds the URI pointing to the location of the image.
    /// </summary>
    public required Uri Url { get; set; }
}