using Wavee.Spotify.Models.Interfaces;
using Wavee.Spotify.Models.Response;
using Wavee.Spotify.Playback;

namespace Wavee.Spotify.Http.Interfaces;

public interface ISpotifyCache
{
    bool TryGet<T>(string id, out T track);
    void Add<T>(string id, T track);

    Task<T?> TryGetOrFetch<T>(string id, Func<string, CancellationToken, Task<T?>> fetch,
        CancellationToken cancellationToken);

    bool TryGetFile(SpotifyAudioFile file, out FileStream o);
    void AddFile(SpotifyAudioFile file, FileStream finalFile);
}