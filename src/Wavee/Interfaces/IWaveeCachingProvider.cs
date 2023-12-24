namespace Wavee.Interfaces;

public interface IWaveeCachingProvider
{
    bool TryGet<T>(string key, out T value);
    void Set<T>(string cacheKey, T result);
    
    bool TryGetFile(string bucket, string key, out Stream value);
}