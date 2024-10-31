namespace Wavee.Interfaces;

/// <summary>
/// Represents the current state of Spotify playback.
/// </summary>
public interface ISpotifyPlaybackState
{
    /// <summary>
    /// Gets the timestamp when the playback state was started
    /// </summary>
    DateTimeOffset? PlayingSinceTimestamp { get; }


    /// <summary>
    /// Getts the timestamp of the last update of the playback state
    /// </summary>
    DateTimeOffset Timestamp { get; }
}