using Wavee.Core.Id;

namespace Wavee.Core.Contracts;

/// <summary>
/// A contract representing an artist for a track across all sources.
/// This is different from <see cref="IArtist"/> in the sense that this contract only represents an artist for a track and does not represent the artist as a whole.
/// Such as the artist's albums, or the artist's top tracks are included in this contract.
/// </summary>
public interface ITrackArtist
{
    /// <summary>
    /// The unique identifier for the artist.
    /// </summary>
    AudioId Id { get; }
    /// <summary>
    /// The name of the artist.
    /// </summary>
    string Name { get; }
}