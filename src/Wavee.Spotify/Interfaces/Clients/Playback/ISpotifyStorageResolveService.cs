using System.Threading.Tasks.Sources;
using Eum.Spotify.storage;
using NeoSmart.AsyncLock;
using Wavee.Spotify.Core.Models.Track;
using Wavee.Spotify.Infrastructure.HttpClients;
using Wavee.Spotify.Infrastructure.Playback;

namespace Wavee.Spotify.Interfaces.Clients.Playback;

internal interface ISpotifyStorageResolveService
{
    Task<SpotifyStreamingFile> GetStorageFile(SpotifyAudioFile file, CancellationToken cancellationToken);
}

internal sealed class SpotifyStreamingFile : IDisposable
{
    private readonly string _cdnUrl;
    private readonly AudioStreamingHttpClient _httpClient;
    private readonly Dictionary<int, byte[]> _chunks = new();
    private readonly AsyncLock _lock = new();

    public SpotifyStreamingFile(
        long totalSize,
        string cdnUrl,
        byte[] firstChunk,
        SpotifyAudioFile file,
        AudioStreamingHttpClient httpClient)
    {
        Length = totalSize;
        _cdnUrl = cdnUrl;
        _httpClient = httpClient;

        _chunks.Add(0, firstChunk);
    }

    public bool Expired => false; //TODO
    public long Length { get; }

    public ValueTask<byte[]> GetChunk(int chunkIndex, CancellationToken cancellationToken)
    {
        if (_chunks.TryGetValue(chunkIndex, out var chunk))
        {
            return new ValueTask<byte[]>(chunk);
        }

        return new ValueTask<byte[]>(GetChunkAsync(chunkIndex, cancellationToken));
    }

    private async Task<byte[]> GetChunkAsync(int chunkIndex, CancellationToken cancellationToken)
    {
        using (await _lock.LockAsync(cancellationToken))
        {
            var (data, _) = await GetChunk(_cdnUrl, _httpClient, chunkIndex, cancellationToken);
            _chunks.Add(chunkIndex, data);
            return data;
        }
    }

    internal static async Task<(byte[] Data, long TotalSize)> GetChunk(string url, AudioStreamingHttpClient client,
        int chunkindex, CancellationToken cancellationToken)
    {
        var start = chunkindex * SpotifyAudioStream.ChunkSize;
        var end = start + SpotifyAudioStream.ChunkSize - 1;
        var chunk = await client.GetRange(url, start, end, cancellationToken);
        return chunk;
    }

    public void Dispose()
    {
        _chunks.Clear();
    }
}