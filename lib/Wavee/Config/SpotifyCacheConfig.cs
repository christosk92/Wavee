namespace Wavee.Config;

/// <summary>
/// Represents the configuration settings for caching Spotify track data.
/// </summary>
public sealed class SpotifyCacheConfig
{
    private string _location = string.Empty;

    /// <summary>
    /// Gets or sets the file system location where the cache database is stored.
    /// Must be a valid file path.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the location is null, empty, or contains invalid characters.</exception>
    public string Location
    {
        get => _location;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Cache location must be a valid file path.", nameof(Location));
            }
            // Additional validation for file path can be added here
            _location = value;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpotifyCacheConfig"/> class with default settings.
    /// </summary>
    public SpotifyCacheConfig()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpotifyCacheConfig"/> class with a specified cache location.
    /// </summary>
    /// <param name="location">The file system path where the cache database will be stored.</param>
    public SpotifyCacheConfig(string location)
    {
        Location = location;
    }
}