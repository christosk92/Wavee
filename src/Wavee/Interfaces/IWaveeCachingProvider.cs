namespace Wavee.Interfaces;

public interface IWaveeCachingProvider
{
    bool TryGet(string key, out byte[] value);
    void Set(string cacheKey, byte[] result);
    
    bool TryGetFile(string bucket, string key, out Stream value);
}