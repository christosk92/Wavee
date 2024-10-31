using Microsoft.Extensions.Logging;

namespace Wavee.Playback.Streaming;

public class CachedChunk
{
    public long Offset { get; }
    public byte[] Data { get; }

    public CachedChunk(long offset, byte[] data)
    {
        Offset = offset;
        Data = data;
    }
}

public class SimplifiedStreamingAudioFileStream : Stream
{
    private readonly Uri _dataSourceUri;
    private long? _length;
    private readonly int _chunkSize;
    private readonly MemoryStream _cache;
    private long _position;
    private bool _endOfStream;
    private readonly HttpClient _httpClient;

    private readonly long _initialOffset; // New field
    private readonly LruCache<long, CachedChunk> _chunkCache;
    private readonly ILogger<WaveeAudioStream> _logger;

    public SimplifiedStreamingAudioFileStream(Uri dataSourceUri, ILogger<WaveeAudioStream> logger,
        int chunkSize = 128 * 1024 * 2 * 2)
    {
        _dataSourceUri = dataSourceUri;
        _logger = logger;
        _chunkSize = chunkSize;
        _chunkCache = new LruCache<long, CachedChunk>(1000);
        _cache = new MemoryStream();
        _position = 0;
        _endOfStream = false;
        _httpClient = new HttpClient();
    }


    public override bool CanRead => true;

    public override bool CanSeek => true; // Now supports seeking

    public override bool CanWrite => false;

    public override long Length => _length ?? 0;

    public override long Position
    {
        get => _position;
        set => Seek(value, SeekOrigin.Begin);
    }

    public long ChunkSize => _chunkSize;

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        long newPosition;

        switch (origin)
        {
            case SeekOrigin.Begin:
                newPosition = offset;
                break;

            case SeekOrigin.Current:
                newPosition = _position + offset;
                break;

            case SeekOrigin.End:
                newPosition = _length!.Value + offset;
                break;

            default:
                throw new ArgumentException("Invalid SeekOrigin", nameof(origin));
        }

        if (newPosition < 0 || newPosition > _length)
            throw new ArgumentOutOfRangeException(nameof(offset), "Attempted to seek outside the stream bounds.");

        _position = newPosition;

        if (_cache.Length > 0)
        {
            if (_position >= _cacheStartPosition && _position < _cacheStartPosition + _cache.Length)
            {
                // The new position is within the cache
                _cache.Position = _position - _cacheStartPosition;
            }
            else
            {
                // Clear cache as it does not contain the data at the new position
                _cache.SetLength(0);
                _cacheStartPosition = _position;
                _endOfStream = false;
            }
        }

        return _position;
    }

    private long _cacheStartPosition; // The position in the overall stream where the cache data starts

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_position >= _length)
            return 0;

        int totalBytesRead = 0;

        while (count > 0)
        {
            long chunkOffset = (_position / _chunkSize) * _chunkSize;

            // Try to get the chunk from the cache
            var cachedChunk = _chunkCache.Get(chunkOffset);
            if (cachedChunk == null)
            {
                // Fetch the chunk from the network
                int bytesToFetch = (int)Math.Min(_chunkSize, _length.Value - chunkOffset);
                var fetchedData = FetchDataAsync(chunkOffset, bytesToFetch, CancellationToken.None).Result;
                if (fetchedData.Length == 0)
                {
                    _endOfStream = true;
                    break;
                }

                cachedChunk = new CachedChunk(chunkOffset, fetchedData);
                _chunkCache.Add(chunkOffset, cachedChunk);
            }

            // Read data from the cached chunk
            int chunkOffsetInBuffer = (int)(_position - chunkOffset);
            int bytesAvailableInChunk = cachedChunk.Data.Length - chunkOffsetInBuffer;
            int bytesToRead = Math.Min(count, bytesAvailableInChunk);
            if (bytesToRead == 0)
                break;

            Array.Copy(cachedChunk.Data, chunkOffsetInBuffer, buffer, offset, bytesToRead);

            _position += bytesToRead;
            offset += bytesToRead;
            count -= bytesToRead;
            totalBytesRead += bytesToRead;
        }

        return totalBytesRead;
    }


    // private async Task<byte[]> FetchDataAsync(long position, int size)
    // {
    //     try
    //     {
    //         long fetchPosition = position + _initialOffset; // Adjust position by initialOffset
    //         _httpClient.DefaultRequestHeaders.Range =
    //             new System.Net.Http.Headers.RangeHeaderValue(fetchPosition, fetchPosition + size - 1);
    //         var response = await _httpClient.GetAsync(_dataSourceUri, HttpCompletionOption.ResponseHeadersRead);
    //         if (response.IsSuccessStatusCode)
    //         {
    //             return await response.Content.ReadAsByteArrayAsync();
    //         }
    //         else
    //         {
    //             return Array.Empty<byte>();
    //         }
    //     }
    //     catch (Exception x)
    //     {
    //         return Array.Empty<byte>();
    //     }
    // }

    public override void SetLength(long value)
    {
        throw new NotSupportedException("SetLength not supported");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException("Write not supported");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cache?.Dispose();
            _httpClient?.Dispose();
        }

        base.Dispose(disposing);
    }

    // Method to check if a chunk is already cached
    public bool IsChunkCached(long chunkOffset)
    {
        return _chunkCache.ContainsKey(chunkOffset);
    }


    // Method to prefetch a specific chunk
    public async Task PrefetchChunk(int number)
    {
        long chunkOffset = number * _chunkSize;
        if (!_chunkCache.ContainsKey(chunkOffset))
        {
            var fetchedData = await FetchDataAsync(chunkOffset, _chunkSize, CancellationToken.None);
            if (fetchedData.Length == 0)
            {
                _endOfStream = true;
                return;
            }

            var cachedChunk = new CachedChunk(chunkOffset, fetchedData);
            _chunkCache.Add(chunkOffset, cachedChunk);
        }
    }

    public long TotalChunks => (_length.Value + _chunkSize - 1) / _chunkSize;

    public bool EverythingFetched(out byte[] bytes)
    {
        long fetchedBytes = 0;
        bytes = new byte[_length.Value];
        foreach (var chunk in _chunkCache.Values)
        {
            Array.Copy(chunk.Data, 0, bytes, chunk.Offset, chunk.Data.Length);
            fetchedBytes += chunk.Data.Length;
        }

        if (fetchedBytes >= _length)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    // Modify FetchDataAsync to accept a CancellationToken
    private async Task<byte[]> FetchDataAsync(long position, int size, CancellationToken cancellationToken)
    {
        try
        {
            long fetchPosition = position + _initialOffset; // Adjust position by initialOffset
            _httpClient.DefaultRequestHeaders.Range =
                new System.Net.Http.Headers.RangeHeaderValue(fetchPosition, fetchPosition + size - 1);
            using var response = await _httpClient.GetAsync(_dataSourceUri, HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _length ??= (response.Content.Headers.ContentRange?.Length ??
                             response.Content.Headers.ContentLength ?? 0) - 0xa7;
                return await response.Content.ReadAsByteArrayAsync(cancellationToken);
            }
            else
            {
                return Array.Empty<byte>();
            }
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            // Operation was canceled
            return Array.Empty<byte>();
        }
        catch (Exception ex)
        {
            // Log exception
            _logger.LogError(ex, $"Failed to fetch data at position {position}.");
            return Array.Empty<byte>();
        }
    }
}