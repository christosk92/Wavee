using LanguageExt;
using Wavee.Core.Ids;

namespace Wavee.Core.Contracts;

/// <summary>
/// A contract representing a track across all sources.
/// </summary>
public interface ITrack
{
    /// <summary>
    /// The unique identifier for the track.
    /// </summary>
    AudioId Id { get; }

    /// <summary>
    /// The title of the track.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// The artists of the track.
    /// </summary>
    Seq<ITrackArtist> Artists { get; }

    /// <summary>
    /// The album of the track.
    /// </summary>
    ITrackAlbum Album { get; }

    /// <summary>
    /// The duration of the track.
    /// </summary>
    TimeSpan Duration { get; }

    /// <summary>
    /// A value indicating whether the track can be played.
    /// This is determined by the source and is usually calculated by checking
    /// if the country of the user is allowed to play the track and/or the track is live.
    /// </summary>
    bool CanPlay { get; }
}