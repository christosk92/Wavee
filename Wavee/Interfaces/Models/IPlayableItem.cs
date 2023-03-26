using System.Collections.Immutable;
using Wavee.Models;

namespace Wavee.Interfaces.Models;

/// <summary>
/// Represents a playable audio item, such as a track or podcast episode,
/// that inherits from <see cref="IAudioItem"/>.
/// </summary>
public interface IPlayableItem : IAudioItem
{
    /// <summary>
    /// Gets a collection of description items associated with the playable item.
    /// For a track this will be the artists.
    /// </summary>
    ImmutableArray<string> Descriptions { get; }
    /// <summary>
    /// Gets the group information for the playable item, such as the album or podcast series it belongs to.
    /// </summary>
    string Group { get; }
    /// <summary>
    /// Gets the duration of the playable item in milliseconds.
    /// </summary>
    double Duration { get; }
}