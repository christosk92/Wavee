using Wavee.Spotify.Models.Interfaces;
using Wavee.Spotify.Models.Response;

namespace Wavee.Spotify.Http.Interfaces;

public interface ISpotifyCache
{
    bool TryGet<T>(string id, out T track) where T : ISpotifyItem;
    void Add<T>(T track) where T : ISpotifyItem;

    Task<T?> TryGetOrFetch<T>(string id, Func<string, CancellationToken, Task<T?>> fetch,
        CancellationToken cancellationToken) where T : ISpotifyItem;
}