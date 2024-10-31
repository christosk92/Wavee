namespace Wavee.Interfaces;

public interface ICachingService
{
    Task<byte[]?> GetAsync(string key);

    // Individual SetAsync
    Task SetAsync(string key, byte[] data, string? etag);

    // Batch GetAsync
    Task<Dictionary<string, byte[]>> GetAsync(IEnumerable<string> keys);
    Task SetAsync(IDictionary<string, (byte[] Value, string? Etag)> items);
}