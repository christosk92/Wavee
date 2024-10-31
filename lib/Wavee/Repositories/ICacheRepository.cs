namespace Wavee.Repositories;

/// <summary>
/// Defines a generic repository for caching data with support for ETags, including individual and batch operations.
/// </summary>
/// <typeparam name="TKey">The type of the cache key.</typeparam>
public interface ICacheRepository<TKey>
{
    // Individual methods
    Task<CacheEntry?> GetAsync(TKey key);
    Task SetAsync(TKey key, CacheEntry entry);

    // Batch methods
    Task<Dictionary<TKey, CacheEntry>> GetAsync(IEnumerable<TKey> keys);
    Task SetAsync(IEnumerable<KeyValuePair<TKey, CacheEntry>> entries);
    Task DeleteAsync(TKey key);
}

/// <summary>
/// Represents a cache entry containing data and an optional ETag.
/// </summary>
public class CacheEntry
{
    public byte[] Data { get; set; } = null!;
    public string? Etag { get; set; }
}