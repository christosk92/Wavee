using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Wavee.Contracts.Interfaces;
using Wavee.Contracts.Interfaces.Contracts;
using Wavee.Contracts.Interfaces.Playback;
using Wavee.UI.Spotify.Playback.Decrypt;

namespace Wavee.UI.Spotify.Playback;

internal sealed class SpotifyMediaSource : IMediaSource
{
    private static HttpClient _httpClient = new();
    private readonly IAudioFile _file;
    private readonly byte[] _key;
    private readonly string _url;

    public SpotifyMediaSource(ITrack item, IAudioFile file, byte[] key, string url)
    {
        Item = item;
        _file = file;
        _key = key;
        _url = url;
    }

    public IPlayableItem Item { get; }

    public async Task<Stream> CreateStream(CancellationToken cancellationToken)
    {
        const int firstChunkStart = 0;
        const int chunkSize = SpotifyConstants.ChunkSize;
        const int firstChunkEnd = firstChunkStart + chunkSize - 1;
        var firstChunk = await GetEncryptedStreamAsync(_url, firstChunkStart, firstChunkEnd, cancellationToken);
        var stream = new SpotifyEncryptedStream(firstChunk.Item1, firstChunk.totalSIze, i =>
        {
            var start = firstChunkStart + i * chunkSize;
            var end = start + chunkSize - 1;
            return GetEncryptedStreamAsync(_url, start, end, cancellationToken);
        });
        var decrypt = new SpotifyDecryptedStream(stream, _key);
        var offsettedStream = new OffsettedStream(decrypt, 0xa7);
        return offsettedStream;
    }

    private static async Task<(byte[], long totalSIze)> GetEncryptedStreamAsync(string url, long start, long end,
        CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Range = new RangeHeaderValue(start, end);
        return await await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ContinueWith(async task =>
            {
                var response = task.Result;
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                return (result, response.Content.Headers.ContentRange.Length.Value);
            }, cancellationToken);
    }
}

internal sealed class OffsettedStream : Stream
{
    private readonly SpotifyDecryptedStream _innerStream;
    private readonly long _offset;
    private long _position;

    public OffsettedStream(SpotifyDecryptedStream innerStream, long offset)
    {
        _innerStream = innerStream;
        _offset = offset;
        _position = 0;

        // Skip the offset bytes initially
        _innerStream.Seek(_offset, SeekOrigin.Begin);
        _position = _offset;
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _innerStream.Length - _offset;

    public override long Position
    {
        get => _position - _offset;
        set => Seek(value, SeekOrigin.Begin);
    }

    public override void Flush()
    {
        
    }

    public override int Read(Span<byte> buffer)
    {
        if (_position < _offset)
        {
            _innerStream.Seek(_offset, SeekOrigin.Begin);
            _position = _offset;
        }
        
        int bytesRead = _innerStream.Read(buffer);
        _position += bytesRead;
        return bytesRead;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        Span<byte> span = buffer.AsSpan(offset, count);
        return Read(span);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        long newPos = origin switch
        {
            SeekOrigin.Begin => offset + _offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => _innerStream.Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, null)
        };

        if (newPos < _offset) throw new ArgumentOutOfRangeException(nameof(offset), "Offset must be positive.");
        _position = _innerStream.Seek(newPos, SeekOrigin.Begin);
        return _position - _offset;
    }

    public override void SetLength(long value)
    {
        
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}