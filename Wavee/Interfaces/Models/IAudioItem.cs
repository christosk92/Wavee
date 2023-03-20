using Wavee.Enums;

namespace Wavee.Interfaces.Models;

/// <summary>
/// Represents a generic audio item, such as a track, album, or artist.
/// </summary>
public interface IAudioItem : IEquatable<IAudioItem>
{
    /// <summary>
    /// Gets the image URL associated with the audio item. <br/>
    /// This can be either a web url, or a file path.
    /// </summary>
    string? Image { get; }

    /// <summary>
    /// Gets the title of the audio item.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the id of the audio item.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the type of service the audio item belongs to.
    /// </summary>
    ServiceType Service { get; }
}
