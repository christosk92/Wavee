using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Infrastructure.Traits;

namespace Wavee.Spotify.Playback.Playback.Streams;

internal sealed class EncryptedSpotifyStream<RT> : IEncryptedSpotifyStream, IDisposable where RT : struct, HasHttp<RT>
{
    private readonly int _numberOfChunks;

    private readonly Option<TaskCompletionSource<ReadOnlyMemory<byte>>>[] _requested;
    private readonly Func<int, Task<ReadOnlyMemory<byte>>> _getChunk;
    private readonly Option<Action<ReadOnlyMemory<byte>>> _finished;

    public EncryptedSpotifyStream(
        ReadOnlyMemory<byte> firstChunk,
        int numberOfChunks,
        long length,
        Func<int, Task<ReadOnlyMemory<byte>>> getChunk,
        Option<Action<ReadOnlyMemory<byte>>> finished)
    {
        _numberOfChunks = numberOfChunks;
        _requested = new Option<TaskCompletionSource<ReadOnlyMemory<byte>>>[numberOfChunks];
        var firstTcs = new TaskCompletionSource<ReadOnlyMemory<byte>>();
        firstTcs.SetResult(firstChunk);
        _requested[0] = Some(firstTcs);

        Length = length;
        _getChunk = getChunk;
        _finished = finished;
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

        //check if we are done
        if (_finished.IsSome)
        {
            if (_requested.All(x => x.IsSome))
            {
                var totalLength = Length;
                Memory<byte> final = new byte[totalLength];
                var offset = 0;
                for (var i = 0; i < _numberOfChunks; i++)
                {
                    var chunkToCopy = _requested[i].ValueUnsafe().Task.Result;
                    chunkToCopy.CopyTo(final.Slice(offset));
                    offset += chunkToCopy.Length;
                }

                _finished.IfSome(action1 => action1(final));
            }
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

    public void Dispose()
    {
        //clear _requested
        _requested.AsSpan().Clear();
    }
}

internal interface IEncryptedSpotifyStream : IDisposable
{
    int NumberOfChunks { get; }
    long Position { get; set; }
    long Length { get; }
    long Seek(long to, SeekOrigin begin);
    int Read(Span<byte> buffer);
}