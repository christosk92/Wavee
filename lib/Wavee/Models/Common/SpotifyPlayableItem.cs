using Spotify.Metadata;

namespace Wavee.Models.Common;

public abstract class SpotifyPlayableItem : SpotifyItem
{
    /// <summary>
    /// Represents the duration of a Spotify playable item.
    /// </summary>
    /// <remarks>
    /// The duration is stored as a <see cref="TimeSpan"/> and represents the length
    /// of the audio track.
    /// </remarks>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Represents the audio files associated with a Spotify playable item.
    /// </summary>
    /// <remarks>
    /// This property contains one or more <see cref="AudioFile"/> objects
    /// which provide metadata and data specific to each file format.
    /// </remarks>
    public AudioFile[] AudioFile { get; set; }
}