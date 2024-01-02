using Wavee.Interfaces;

namespace Wavee;

public class NullCachingService : IWaveeCachingProvider
{    
    private readonly Dictionary<string, byte[]> _cache = new();

    public static NullCachingService Instance { get; } = new NullCachingService();


    public bool TryGet(string key, out byte[] value)
    {
        if (_cache.TryGetValue(key, out value))
        {
            return true;
        }
        
        value = default;
        return false;
    }

    public void Set(string cacheKey, byte[] result)
    {
        _cache[cacheKey] = result;
    }

    public bool TryGetFile(string bucket, string key, out Stream value)
    {
        value = default;
        return false;
    }
}