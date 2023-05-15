using System.Diagnostics;
using System.Net.Http.Headers;
using LanguageExt;
using Wavee.Core.Infrastructure.Traits;
using Wavee.Spotify.Playback.Playback;

namespace Wavee.Spotify.Playback;

internal abstract class EncryptedSpotifyStream : Stream
{
    private long _length;

    protected EncryptedSpotifyStream(long length)
    {
        _length = length;
    }

    public override void Flush()
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, null)
        };
        Position = position;
        return position;
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _length;
    public override long Position { get; set; }
}

internal sealed class HttpEncryptedSpotifyStream<R> : EncryptedSpotifyStream where R : struct, HasHttp<R>
{
    private readonly string _cdnUrl;
    private static readonly HttpClient _client = new HttpClient();

    private AtomHashMap<int, TaskCompletionSource<ReadOnlyMemory<byte>>> _chunks =
        LanguageExt.AtomHashMap<int, TaskCompletionSource<ReadOnlyMemory<byte>>>.Empty;

    public HttpEncryptedSpotifyStream(string cdnUrl, int minimumDownloadSize, long length) : base(length)
    {
        _cdnUrl = cdnUrl;
        const int chunkSize = SpotifyPlaybackConstants.ChunkSize;
        var chunks = (int)Math.Ceiling((double)length / chunkSize);
        for (var i = 0; i < chunks; i++)
        {
            var chunk = i;
            var tcs = new TaskCompletionSource<ReadOnlyMemory<byte>>(TaskCreationOptions
                .RunContinuationsAsynchronously);
            _chunks.Add(chunk, tcs);
            // _chunks = _chunks.AddOrUpdate(chunk, tcs);
            _ = Task.Run(async () =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, _cdnUrl);
                request.Headers.Range = new RangeHeaderValue(chunk * chunkSize,
                    chunk == chunks - 1 ? length - 1 : chunk * chunkSize + chunkSize - 1);
                using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                var stream = await response.Content.ReadAsByteArrayAsync();
                tcs.SetResult(stream);
            });
        }
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var chunkIndex = (int)(Position / SpotifyPlaybackConstants.ChunkSize);
        var chunkOffset = (int)(Position % SpotifyPlaybackConstants.ChunkSize);
        var chunkSize = (int)Math.Min(count, SpotifyPlaybackConstants.ChunkSize - chunkOffset);
        if (chunkIndex >= _chunks.Count)
        {
            return 0;
        }
        
        var chunk = await _chunks[chunkIndex].Task;
        try
        {
            chunk.Slice(0, chunkSize).CopyTo(buffer.AsMemory(offset, chunkSize));
        }
        catch (Exception e)
        {
            Debugger.Break();
            throw;
        }

        Position += chunkSize;
        return chunkSize;
    }


    public override int Read(byte[] buffer, int offset, int count)
    {
        return ReadAsync(buffer, offset, count).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _client?.Dispose();
        }

        base.Dispose(disposing);
    }
}