using System.Collections.Immutable;

namespace Wavee.Interfaces.Models;


/// <summary>
/// Represents the base contract for a track originating from any service.
/// Inherits from <see cref="IPlayableItem"/>.
/// </summary>
public interface ITrack : IPlayableItem
{
    /// <summary>
    /// Gets the <see cref="IAlbum"/> instance representing the album the track belongs to.
    /// </summary>
    IAlbum Album { get; }

    /// <summary>
    /// Gets an array of <see cref="IArtist"/> instances representing the artists contributing to the track.
    /// </summary>
    ImmutableArray<IArtist> Artists { get; }
}

/// <summary>
/// Represents the base contract for an artist originating from any service.
/// Inherits from <see cref="IAudioItem"/>.
/// </summary>
public interface IArtist : IAudioItem
{

}

/// <summary>
/// Represents the base contract for an album originating from any service.
/// Inherits from <see cref="IAudioItem"/>.
/// </summary>
public interface IAlbum : IAudioItem
{
}