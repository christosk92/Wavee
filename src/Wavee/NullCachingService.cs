using Wavee.Interfaces;

namespace Wavee;

public class NullCachingService : IWaveeCachingProvider
{
    public static NullCachingService Instance { get; } = new NullCachingService();

    public bool TryGet<T>(string key, out T value)
    {
        value = default;
        return false;
    }

    public void Set<T>(string cacheKey, T result)
    {
        // nothing
    }

    public bool TryGetFile(string bucket, string key, out Stream value)
    {
        value = default;
        return false;
    }
}