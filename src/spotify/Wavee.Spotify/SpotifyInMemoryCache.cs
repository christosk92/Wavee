using Microsoft.Extensions.Caching.Memory;
using Wavee.Spotify.Http.Interfaces;
using Wavee.Spotify.Models.Interfaces;
using Wavee.Spotify.Models.Response;

namespace Wavee.Spotify;

internal sealed class SpotifyInMemoryCache : ISpotifyCache
{
    private readonly MemoryCache _cache;
    private static TimeSpan _expiration = TimeSpan.FromMinutes(60);
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    private SpotifyInMemoryCache()
    {
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 1_000_000,
            ExpirationScanFrequency = TimeSpan.FromSeconds(30)
        });
    }

    public static SpotifyInMemoryCache Instance { get; } = new SpotifyInMemoryCache();

    public bool TryGet<T>(string id, out T track) where T : ISpotifyItem => _cache.TryGetValue(id, out track);

    public void Add<T>(T track) where T : ISpotifyItem
    {
        _cache.Set(track.Uri.ToString(), track, new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(DateTimeOffset.Now.Add(_expiration))
            .SetSize(1)); 
    }

    public async Task<T?> TryGetOrFetch<T>(string id, Func<string, CancellationToken, Task<T?>> fetchTrack,
        CancellationToken cancellationToken) where T : ISpotifyItem
    {
        if (!TryGet(id, out T? track))
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                // Check again if the track was added to the cache while waiting for the semaphore.
                if (!TryGet(id, out track))
                {
                    // Fetch the track if it's not in the cache.
                    track = await fetchTrack(id, cancellationToken);
                    if (track is null) return default;

                    // Add the fetched track to the cache.
                    Add(track);
                }
            }
            catch
            {
                // If an exception occurs, remove the track from the cache.
                _cache.Remove(id);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        return track;
    }
}