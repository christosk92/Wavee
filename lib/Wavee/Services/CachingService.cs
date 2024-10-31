// CachingService.cs

using Microsoft.Extensions.Logging;
using Wavee.Repositories;
using Wavee.Interfaces;

namespace Wavee.Services;

public class CachingService : ICachingService
{
    private readonly ICacheRepository<string> _cacheRepository;
    private readonly ILogger<CachingService> _logger;

    public CachingService(ICacheRepository<string> cacheRepository, ILogger<CachingService> logger)
    {
        _cacheRepository = cacheRepository ?? throw new ArgumentNullException(nameof(cacheRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Individual GetAsync
    public async Task<byte[]?> GetAsync(string key)
    {
        var entry = await _cacheRepository.GetAsync(key);
        return entry?.Data;
    }

    // Individual SetAsync
    public async Task SetAsync(string key, byte[] data, string? etag)
    {
        var entry = new CacheEntry { Data = data, Etag = etag };
        await _cacheRepository.SetAsync(key, entry);
    }

    // Batch GetAsync
    public async Task<Dictionary<string, byte[]>> GetAsync(IEnumerable<string> keys)
    {
        var cacheEntries = await _cacheRepository.GetAsync(keys);
        var results = new Dictionary<string, byte[]>();

        foreach (var kvp in cacheEntries)
        {
            results[kvp.Key] = kvp.Value.Data;
        }

        return results;
    }

    // Batch SetAsync
    public async Task SetAsync(IDictionary<string, (byte[] Value, string? Etag)> items)
    {
        var entries = items.Select(kvp =>
            new KeyValuePair<string, CacheEntry>(
                kvp.Key,
                new CacheEntry
                {
                    Data = kvp.Value.Value,
                    Etag = kvp.Value.Etag
                }
            )
        );
        await _cacheRepository.SetAsync(entries);
    }
}