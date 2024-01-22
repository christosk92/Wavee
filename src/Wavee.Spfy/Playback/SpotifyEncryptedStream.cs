using System.Threading.Tasks.Sources;
using LanguageExt;

namespace Wavee.Spfy.Playback;

internal sealed class SpotifyEncryptedStream
{
    public readonly long TotalSize;
    private readonly Dictionary<int, ValueTask<byte[]>> _chunks = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Func<int, Task<(byte[] bytes, long totalSize)>> _func;

    public SpotifyEncryptedStream(byte[] firstChunk, long totalSize,
        Func<int, Task<(byte[] bytes, long totalSize)>> func)
    {
        TotalSize = totalSize;
        _chunks.Add(0, new ValueTask<byte[]>(firstChunk));
        // _chunks[0] = new ValueTask<byte[]>(firstChunk);
        _func = func;
    }

    public byte[] GetChunk(int chunkIndex, bool preloading, CancellationToken cancellationToken)
    {
        try
        {
            if (preloading)
            {
                _semaphore.Wait(cancellationToken);
            }

            if (_chunks.TryGetValue(chunkIndex, out var chunk) && !preloading)
            {
                if (chunk.IsCompleted)
                {
                    return chunk.Result;
                }
                else
                {
                    return Task.Run(async () => await chunk, cancellationToken).ConfigureAwait(false).GetAwaiter()
                        .GetResult();
                }
            }

            if (preloading && !_chunks.ContainsKey(chunkIndex))
            {
                _chunks.Add(chunkIndex, new ValueTask<byte[]>(FetchAsync(chunkIndex)));
                // _chunks[chunkIndex] = new ValueTask<byte[]>(FetchAsync(chunkIndex));
                return null;
            }
            else if (preloading)
            {
                return null;
            }

            var (bytes, _) = Task.Run(async () => await _func(chunkIndex), cancellationToken).ConfigureAwait(false)
                .GetAwaiter().GetResult();
            _chunks.Add(chunkIndex, new ValueTask<byte[]>(bytes));
            return bytes;
        }
        finally
        {
            if (preloading)
            {
                _semaphore.Release();
            }
        }
    }

    private async Task<byte[]> FetchAsync(int chunkIndex)
    {
        return await Task.Run(async () =>
        {
            var res = await _func(chunkIndex).Map(x => x.bytes);
            return res;
        });
    }
}