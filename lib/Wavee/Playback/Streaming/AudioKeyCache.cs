using System.Collections.Concurrent;
using Wavee.Models.Common;

namespace Wavee.Playback.Streaming;

public static class AudioKeyCache
{
    private static readonly ConcurrentDictionary<string, byte[]> _cache = new ConcurrentDictionary<string, byte[]>();

    public static bool TryGetValue(string keyId, out byte[] key)
    {
        return _cache.TryGetValue(keyId, out key);
    }

    public static void Add(string keyId, byte[] key)
    {
        _cache[keyId] = key;
    }
}

public static class CdnUrlCache
{
    private static readonly ConcurrentDictionary<FileId, CdnUrl> _cache =
        new ConcurrentDictionary<FileId, CdnUrl>();

    public static bool TryGetValue(FileId fileId, out CdnUrl urls)
    {
        if (_cache.TryGetValue(fileId, out urls))
        {
            if(urls.TryGetUrl(out _))
            {
                return true;
            }
            
            // if we don't have a valid url, remove the entry
            _cache.TryRemove(fileId, out _);
        }
        
        return false;
    }

    public static void Add(FileId fileId, CdnUrl urls)
    {
        _cache[fileId] = urls;
    }
}