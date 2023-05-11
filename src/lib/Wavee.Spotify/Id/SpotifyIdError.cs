namespace Wavee.Spotify.Id;

public enum SpotifyIdError
{
    /// <summary>
    ///     ID cannot be parsed.
    /// </summary>
    InvalidId,

    /// <summary>
    ///     Not a valid Spotify URI
    /// </summary>
    InvalidFormat,

    /// <summary>
    ///     URI does not belong to Spotify
    /// </summary>
    InvalidRoot
}