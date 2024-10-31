namespace Wavee.Config;

/// <summary>
/// Represents a cache for storing and retrieving Spotify authentication credentials.
/// </summary>
public sealed class SpotifyCredentialsCache
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SpotifyCredentialsCache"/> class with specified credential retrieval and storage delegates.
    /// </summary>
    /// <param name="retrieveCredentials">A delegate to retrieve stored credentials.</param>
    /// <param name="storeCredentials">A delegate to store new credentials.</param>
    public SpotifyCredentialsCache(RetrieveCredentialsDelegate? retrieveCredentials,
        StoreCredentialsDelegate? storeCredentials)
    {
        RetrieveCredentials = retrieveCredentials;
        StoreCredentials = storeCredentials;
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SpotifyCredentialsCache"/> class with no delegates.
    /// </summary>
    public SpotifyCredentialsCache()
    {
    }
    
    /// <summary>
    /// Gets the delegate responsible for retrieving stored Spotify credentials.
    /// </summary>
    public RetrieveCredentialsDelegate? RetrieveCredentials { get; }
    
    /// <summary>
    /// Gets the delegate responsible for storing new Spotify credentials.
    /// </summary>
    public StoreCredentialsDelegate? StoreCredentials { get; }
}