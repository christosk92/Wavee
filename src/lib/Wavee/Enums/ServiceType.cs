namespace Wavee.Enums;

/// <summary>
/// An enum indicating the source of the particular audio item.
/// </summary>
public enum ServiceType
{
    /// <summary>
    /// Item originated from somewhere local.
    /// </summary>
    Local,
    /// <summary>
    /// Item originated from Spotify.
    /// <br/>
    /// Note: Some tracks in spotify are local:track: These items will still have a ItemType of Spotify, but the source type in the SpotifyId will most likely be local.
    /// </summary>
    Spotify
}