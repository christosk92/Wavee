namespace Wavee.Enums;

/// <summary>
/// Represents the different types of Spotify tokens.
/// </summary>
public enum SpotifyTokenType
{
    /// <summary>
    /// Represents a bearer token type used for Spotify API authentication.
    /// </summary>
    Bearer,

    /// <summary>
    /// Represents a client token. A client token is an extra type of token that can be passed to some API calls.
    /// It is not required (yet)
    /// </summary>
    ClientToken
}