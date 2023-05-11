using LanguageExt.UnsafeValueAccess;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Clients.Mercury.Metadata;
using Wavee.Spotify.Clients.Playback.Cdn;

namespace Wavee.Spotify.Clients.Playback.Streams;

internal sealed class EncryptedSpotifyStream<RT> where RT : struct, HasHttp<RT>
{
    private readonly MaybeExpiringUrl _cdnUrl;
    private readonly TrackOrEpisode _metadata;
    private readonly int _numberOfChunks;

    private readonly Option<TaskCompletionSource<ReadOnlyMemory<byte>>>[] _requested;
    private readonly Func<int, Task<ReadOnlyMemory<byte>>> _getChunk;

    public EncryptedSpotifyStream(
        MaybeExpiringUrl cdnUrl,
        TrackOrEpisode metadata,
        ReadOnlyMemory<byte> firstChunk,
        int numberOfChunks,
        long length, Func<int, Task<ReadOnlyMemory<byte>>> getChunk)
    {
        _cdnUrl = cdnUrl;
        _metadata = metadata;
        _numberOfChunks = numberOfChunks;
        _requested = new Option<TaskCompletionSource<ReadOnlyMemory<byte>>>[numberOfChunks];
        var firstTcs = new TaskCompletionSource<ReadOnlyMemory<byte>>();
        firstTcs.SetResult(firstChunk);
        _requested[0] = Some(firstTcs);

        Length = length;
        _getChunk = getChunk;
    }

    public long Position { get; set; }
    public long Length { get; }
    public int NumberOfChunks => _numberOfChunks;

    public int Read(Span<byte> buffer)
    {
        var chunkToRead = (int)(Position / SpotifyPlaybackConstants.ChunkSize);
        var chunkOffset = (int)(Position % SpotifyPlaybackConstants.ChunkSize);

        var chunkFinal = FetchChunk(chunkToRead).ConfigureAwait(false).GetAwaiter().GetResult();

        var firstChunkToRead =
            chunkFinal.Slice(chunkOffset, Math.Min(buffer.Length, chunkFinal.Length - chunkOffset));
        firstChunkToRead.Span.CopyTo(buffer);
        Position += firstChunkToRead.Length;
        return firstChunkToRead.Length;
    }

    private async ValueTask<ReadOnlyMemory<byte>> FetchChunk(int index, int preloadAhead = 2)
    {
        ReadOnlyMemory<byte> chunk;
        if (_requested[index].IsNone)
        {
            var tcs = new TaskCompletionSource<ReadOnlyMemory<byte>>();
            _requested[index] = Some(tcs);
            chunk = await _getChunk(index);
            tcs.SetResult(chunk);
        }
        else
        {
            chunk = await _requested[index].ValueUnsafe().Task;
        }

        if (index + preloadAhead < _numberOfChunks)
        {
            for (var i = index + 1; i < index + preloadAhead; i++)
            {
                if (_requested[i].IsNone)
                {
                    var tcs = new TaskCompletionSource<ReadOnlyMemory<byte>>();
                    _requested[i] = tcs;
                    var i1 = i;
                    _ = Task.Run(async () =>
                    {
                        var preloaded = await _getChunk(i1);
                        tcs.SetResult(preloaded);
                    });
                }
            }

            ;
        }

        return chunk;
    }

    // public int Read(byte[] buffer, int offset, int count)
    // {
    //     var chunkToRead = (int)(Position / SpotifyPlaybackRuntime.ChunkSize);
    //     var chunkOffset = (int)(Position % SpotifyPlaybackRuntime.ChunkSize);
    //
    //     if (chunkToRead == 0)
    //     {
    //         var firstChunkToRead = _firstChunk.Slice(chunkOffset, Math.Min(count, _firstChunk.Length - chunkOffset));
    //         firstChunkToRead.CopyTo(buffer.AsMemory(offset, firstChunkToRead.Length));
    //         Position += firstChunkToRead.Length;
    //         return firstChunkToRead.Length;
    //     }
    //     //check if in cache
    //
    //     return 0;
    // }


    public long Seek(long to, SeekOrigin begin)
    {
        switch (begin)
        {
            case SeekOrigin.Begin:
                Position = to;
                break;
            case SeekOrigin.Current:
                Position += to;
                break;
            case SeekOrigin.End:
                Position = Length - to;
                break;
        }

        return Position;
    }
}