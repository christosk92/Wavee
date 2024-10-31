namespace Wavee.Repositories;

/// <summary>
/// Represents a cache entry containing data, content type, and ETag.
/// </summary>
public class CacheItem
{
    /// <summary>
    /// Gets or sets the cached data as a byte array.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the Content-Type of the cached data.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the ETag associated with the cached data.
    /// </summary>
    public string? ETag { get; set; }
}