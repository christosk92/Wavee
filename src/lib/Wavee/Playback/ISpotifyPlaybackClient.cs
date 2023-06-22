namespace Wavee.Playback;

/// <summary>
/// A client for interacting with the Spotify playback.
/// </summary>
public interface ISpotifyPlaybackClient
{
    /// <summary>
    /// Takes over the playback to this client, and returns whether or not it was successful.
    /// This also notifies the Spotify app that the playback has been taken over.
    /// </summary>
    /// <returns>
    /// A boolean indicating whether or not the takeover was successful.
    /// Note that this does not indicate whether an error occurred during the takeover.
    /// In case of an error, an exception will be thrown.
    ///
    /// False usually means there was no playback to take over.
    /// </returns>
    Task<bool> Takeover();
}