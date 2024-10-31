namespace Wavee.Interfaces;

/// <summary>
/// Represents the primary client for interacting with Spotify services, including token management, playback control, and API interactions.
/// </summary>
public interface ISpotifyClient
{
    /// <summary>
    /// 
    /// </summary>
    IWaveePlayer Player { get; }
    
    /// <summary>
    /// Gets the Spotify token client responsible for handling authentication and token-related operations.
    /// </summary>
    ISpotifyTokenClient TokenClient { get; }

    /// <summary>
    /// Gets the Spotify playback client responsible for managing playback operations and monitoring playback state.
    /// </summary>
    ISpotifyPlaybackClient PlaybackClient { get; }

    /// <summary>
    /// Gets the Spotify API client responsible for interacting with Spotify's Web API for various operations.
    /// </summary>
    ISpotifyApiClient ApiClient { get; }

    /// <summary>
    /// Gets the Spotify library client responsible for interacting with the user's Spotify library for managing saved tracks, albums, and playlists.
    /// </summary>
    ISpotifyLibraryClient LibraryClient { get; }

    /// <summary>
    /// Gets the Spotify playlist client responsible for interacting with the user's Spotify playlists for managing tracks and playlist metadata.
    /// </summary>
    ISpotifyPlaylistClient PlaylistClient { get; }

    /// <summary>
    /// A unique identifier for the current user.
    /// Note: This value is not guaranteed to be available immediately after initialization and may require an asynchronous operation to retrieve.
    /// </summary>
    ValueTask<string> UserId();
}