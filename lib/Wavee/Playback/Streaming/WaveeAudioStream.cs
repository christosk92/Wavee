using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using NeoSmart.AsyncLock;
using Spotify.Metadata;
using Wavee.Interfaces;
using Wavee.Models.Common;
using Wavee.Playback.Player;

namespace Wavee.Playback.Streaming;

public class WaveeAudioStream : AudioStream
{
    private static readonly string _cacheDirectory = Path.Combine(Path.GetTempPath(), "WaveeCache");


    private static readonly byte[] iv = new byte[]
    {
        0x72, 0xe0, 0x67, 0xfb, 0xdd, 0xcb, 0xcf, 0x77,
        0xeb, 0xe8, 0xbc, 0x64, 0x3f, 0x63, 0x0d, 0x93
    };

    private readonly SpotifyId _trackId;
    private FileId _fileId;
    private readonly IAudioKeyManager _audioKeyManager;
    private readonly ISpotifyApiClient _spotifyApiClient;
    private long _position;
    private long _length;

    private readonly object _lock = new object();
    private bool _cdnReady = false;

    private Exception _initializationException;

    private Stream _headStream;
    private AesCtrStream _decryptedCdnStream;
    private NormalizationData? _normalizationData;
    private static readonly HttpClient _httpClient = new HttpClient();
    private readonly ILogger<WaveeAudioStream> _logger;

    // Buffering parameters
    // private readonly TimeSpan _bufferTriggerPoint = TimeSpan.FromSeconds(30); // Start buffering after 30 seconds
    // private readonly TimeSpan _bufferInterval = TimeSpan.FromSeconds(10); // Interval between prefetching
    // private readonly int _initialPrefetchChunks = 2; // Start with 2 chunks
    // private readonly int _maxPrefetchChunks = 1024; // Maximum number of chunks to prefetch
    // private int _currentPrefetchMultiplier = 1; // Multiplier for exponential growth
    // private bool _bufferingStarted = false;
    // private CancellationTokenSource _bufferingCancellationTokenSource;
    // private long _prefetchedChunks = 0; // Counter for prefetched chunks
    //
    // private Task _bufferingTask;


    internal WaveeAudioStream(
        WaveePlayerMediaItem mediaItem,
        SpotifyId trackId,
        IAudioKeyManager audioKeyManager,
        ISpotifyApiClient spotifyApiClient,
        ILogger<WaveeAudioStream> logger) : base(mediaItem)
    {
        _trackId = trackId;
        _audioKeyManager = audioKeyManager;
        _spotifyApiClient = spotifyApiClient;
        _logger = logger;
        _position = 0;
        Directory.CreateDirectory(_cacheDirectory);
    }

    private AsyncLock _LL = new AsyncLock();

    private string GetCacheFilePath()
    {
        return Path.Combine(_cacheDirectory, $"{_fileId.ToBase16()}.dat");
    }

    public Task AwaitFulInit => _initTask;

    public override async Task InitializeAsync(CancellationToken cancellationToken)
    {
        using (await _LL.LockAsync())
        {
            var trackMetadata = await _spotifyApiClient.GetTrack(_trackId, true, cancellationToken);
            Track = trackMetadata;
            TotalDuration = trackMetadata.Duration;
            var fileId = trackMetadata.AudioFile.FirstOrDefault(f => f.Format is AudioFile.Types.Format.OggVorbis320)
                ?.FileId;
            if (fileId is null)
            {
                throw new Exception("No audio file found.");
            }

            _fileId = FileId.FromAudioFile(fileId.ToByteArray());

            // string cacheFilePath = GetCacheFilePath();
            // if (File.Exists(cacheFilePath))
            // {
            //     // Use the cached file
            //     _headStream = new FileStream(cacheFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            //     _length = _headStream.Length;
            //     _canSeek = true;
            //     _cdnReady = true;
            //     return;
            // }

            // if (_cacheFileStream == null)
            // {
            //     string tempCacheFilePath = GetCacheFilePath() + ".tmp";
            //     _cacheFileStream = new FileStream(tempCacheFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            // }

            // Fetch the head stream synchronously
            FetchHeadStream();

            // Start asynchronous initialization for the rest
            _ = Task.Run(() => InitInternal(cancellationToken), cancellationToken);
        }

        // _bufferingCancellationTokenSource = new CancellationTokenSource();
        // _bufferingTask = Task.Run(() => BufferAheadAsync(_bufferingCancellationTokenSource.Token));
    }

    private void FetchHeadStream()
    {
        try
        {
            string headUrl = $"https://heads-ak-spotify-com.akamaized.net/head/{_fileId.ToBase16()}";
            var headData = _httpClient.GetByteArrayAsync(headUrl).Result; // Synchronous wait
            ExtractNormalizationData(headData);

            // Skip the first 0xA7 bytes
            if (headData.Length <= 0xA7)
            {
                _headStream = Stream.Null; // Empty stream
            }
            else
            {
                _headStream = new MemoryStream(headData, 0xA7, headData.Length - 0xA7, writable: false);
            }

            // Update length with head stream length
            _canSeek = false;
            _length = _headStream.Length;
        }
        catch (Exception ex)
        {
            _initializationException = ex;
        }
    }

    // private async Task BufferAheadAsync(CancellationToken cancellationToken)
    // {
    //     try
    //     {
    //         while (!cancellationToken.IsCancellationRequested)
    //         {
    //             if (_sampleProvider is null)
    //             {
    //                 await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
    //                 continue;
    //             }
    //
    //             // Check if buffering has already started
    //             if (!_bufferingStarted)
    //             {
    //                 TimeSpan currentPlaybackTime = _sampleProvider.CurrentTime;
    //                 if (currentPlaybackTime >= _bufferTriggerPoint)
    //                 {
    //                     _bufferingStarted = true;
    //                     _logger.LogInformation("Buffering started.");
    //                 }
    //             }
    //             else
    //             {
    //                 int chunksToPrefetch = _initialPrefetchChunks * _currentPrefetchMultiplier;
    //                 var fetchedEverything = await PrefetchChunksAsync(chunksToPrefetch, cancellationToken);
    //                 if (fetchedEverything)
    //                 {
    //                     _logger.LogInformation("All chunks have been prefetched. Stopping buffering.");
    //                     break; // Exit the buffering loop
    //                 }
    //
    //                 _logger.LogInformation($"Prefetched {_prefetchedChunks} chunks ahead.");
    //
    //                 // Check if all chunks have been prefetched
    //                 if (_prefetchedChunks >=
    //                     ((_decryptedCdnStream.BaseStream as SimplifiedStreamingAudioFileStream)?.TotalChunks ?? 0))
    //                 {
    //                     _logger.LogInformation("All chunks have been prefetched. Stopping buffering.");
    //                     break; // Exit the buffering loop
    //                 }
    //
    //                 // Double the multiplier for exponential growth, respecting the maximum limit
    //                 _currentPrefetchMultiplier = Math.Min(_currentPrefetchMultiplier * 2,
    //                     _maxPrefetchChunks / _initialPrefetchChunks);
    //
    //                 // Wait for the specified interval before the next prefetch
    //                 await Task.Delay(_bufferInterval, cancellationToken);
    //             }
    //
    //             // Short delay to prevent tight loop
    //             await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
    //         }
    //     }
    //     catch (OperationCanceledException)
    //     {
    //         // Buffering was canceled
    //         _logger.LogInformation("Buffering task was canceled.");
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "An error occurred in the buffering task.");
    //     }
    // }

    private long _chunkSize;

    private async Task InitInternal(CancellationToken cancellationToken)
    {
        try
        {
            // Begin fetching CDN URL and decryption key

            Task<byte[]> decryptionKeyTask;
            string keyId = _fileId.ToBase16();

            if (!AudioKeyCache.TryGetValue(keyId, out var decryptionKeyCached))
            {
                decryptionKeyTask = _audioKeyManager.RequestAsync(_trackId, _fileId)
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            throw t.Exception;
                        }

                        var decryptionKey = t.Result.Key;
                        AudioKeyCache.Add(keyId, decryptionKey);
                        return decryptionKey;
                    }, cancellationToken);
            }
            else
            {
                decryptionKeyTask = Task.FromResult(decryptionKeyCached);
            }

            // check if the file is already cached
            string cacheFilePath = GetCacheFilePath();
            if (File.Exists(cacheFilePath))
            {
                var fs = new FileStream(cacheFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                _decryptedCdnStream = new AesCtrStream(fs,
                    decryptionKeyTask.Result,
                    iv);
                _length = fs.Length - 0xA7;
                _canSeek = true;
                _cdnReady = true;
                return;
            }

            var cdnUrlTask = Task.Run(async () =>
            {
                var fileId = FileId.FromAudioFile(_fileId.ToByteArray());
                if (CdnUrlCache.TryGetValue(fileId, out var url))
                {
                    return url.TryGetUrl(out var u) ? u : null;
                }

                var cdnUrl = new CdnUrl(FileId.FromAudioFile(_fileId.ToByteArray()));
                var resolvedCdnUrl = await cdnUrl.ResolveAudio(_spotifyApiClient);
                CdnUrlCache.Add(fileId, resolvedCdnUrl);
                return resolvedCdnUrl.TryGetUrl(out var uv) ? uv : null;
            }, cancellationToken);

            await Task.WhenAll(cdnUrlTask, decryptionKeyTask);

            var cdnUrl = cdnUrlTask.Result;
            byte[] decryptionKey = decryptionKeyTask.Result;

            // Create CDN stream starting from the offset after the initial 0xA7 bytes
            var cdnStream = new SimplifiedStreamingAudioFileStream(
                new Uri(cdnUrl),
                logger: _logger);
            _chunkSize = cdnStream.ChunkSize;
            await cdnStream.PrefetchChunk(0);


            // Adjust the total length by subtracting the first 0xA7 bytes
            //long adjustedFileLength = totalFileLength - 0xA7;

            // Update the total length of the stream
            lock (_lock)
            {
                _length = cdnStream.Length;
                _canSeek = true;
            }

            // Calculate initialCounter and initialOffsetInBlock based only on 0xA7 bytes
            long initialCounter = 0xA7 / 16; // Block size is 16 bytes
            int initialOffsetInBlock = (int)(0xA7 % 16);

            // Wrap with AesCtrStream for decryption, passing the initialCounter
            _decryptedCdnStream = new AesCtrStream(cdnStream, decryptionKey, iv);


            lock (_lock)
            {
                _cdnReady = true;
                Monitor.PulseAll(_lock);
            }

            _logger.LogInformation("CDN stream is ready.");
        }
        catch (Exception ex)
        {
            _initializationException = ex;
            lock (_lock)
            {
                Monitor.PulseAll(_lock);
            }
        }
    }

    // private async Task<bool> PrefetchChunksAsync(int numberOfChunks, CancellationToken cancellationToken)
    // {
    //     bool fetchedEverything = false;
    //     if (_decryptedCdnStream.BaseStream is SimplifiedStreamingAudioFileStream streamingStream)
    //     {
    //         long chunkSize = streamingStream.ChunkSize;
    //         long totalChunks = streamingStream.TotalChunks;
    //
    //         // Calculate the starting chunk index based on the current position
    //         long currentChunkIndex = _position / chunkSize;
    //         long prefetchStartIndex = currentChunkIndex + 1; // Start after the current chunk
    //
    //         for (int i = 0; i < numberOfChunks; i++)
    //         {
    //             if (cancellationToken.IsCancellationRequested)
    //                 break;
    //
    //             long chunkIndex = prefetchStartIndex + i;
    //             long chunkOffset = chunkIndex * chunkSize;
    //
    //             if (chunkIndex >= totalChunks)
    //                 return true;
    //
    //             if (!streamingStream.IsChunkCached(chunkOffset))
    //             {
    //                 await streamingStream.PrefetchChunkAsync(chunkOffset, cancellationToken);
    //                 _logger.LogDebug($"Prefetched chunk {chunkIndex + 1}/{totalChunks} at offset {chunkOffset}.");
    //                 Interlocked.Increment(ref _prefetchedChunks);
    //             }
    //         }
    //     }
    //
    //     return fetchedEverything;
    // }

    private void ExtractNormalizationData(byte[] data)
    {
        const int normalizationDataOffset = 0x90; // 144 in decimal
        const int normalizationDataSize = 16;

        if (data.Length < normalizationDataOffset + normalizationDataSize)
        {
            // Not enough data to extract normalization info
            _normalizationData = NormalizationData.Default;
            return;
        }

        byte[] normalizationBytes = new byte[normalizationDataSize];
        Array.Copy(data, normalizationDataOffset, normalizationBytes, 0, normalizationDataSize);

        // Parse the normalization data
        _normalizationData = new NormalizationData
        {
            TrackGainDb = BitConverter.ToSingle(normalizationBytes, 0),
            TrackPeak = BitConverter.ToSingle(normalizationBytes, 4),
            AlbumGainDb = BitConverter.ToSingle(normalizationBytes, 8),
            AlbumPeak = BitConverter.ToSingle(normalizationBytes, 12)
        };
    }

    private bool _first = true;
    private bool _canSeek;
    private Task _initTask;
    private bool _loggedReadingFromHead = false;
    private bool _loggedReadingFromCdn = false;

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_initializationException != null)
        {
            throw _initializationException;
        }

        int bytesRead = 0;

        //Read from head stream if position is within its length
        if (_position < _headStream.Length)
        {
            _headStream.Position = _position;
            int bytesToRead = (int)Math.Min(count, _headStream.Length - _position);
            int read = _headStream.Read(buffer, offset, bytesToRead);
            bytesRead += read;
            _position += read;
            offset += read;
            count -= read;
            if (!_loggedReadingFromHead)
            {
                _logger.LogInformation("Reading from head stream.");
                _loggedReadingFromHead = true;
            }
        }

        // Read from decrypted CDN stream if more data is requested
        // fetch max 1 chunk
        if (count > 0)
        {
            if (!_loggedReadingFromCdn)
            {
                _logger.LogInformation("Reading from CDN stream for {TrackName}", Track.Name);
                _loggedReadingFromCdn = true;
            }

            // Wait until the CDN stream is ready
            lock (_lock)
            {
                while (!_cdnReady && _initializationException == null)
                {
                    Monitor.Wait(_lock);
                }

                if (_initializationException != null)
                {
                    throw _initializationException;
                }
            }

            // Adjust position for decrypted CDN stream
            long cdnPosition = _position + 0xA7;
            _decryptedCdnStream.Seek(cdnPosition, SeekOrigin.Begin);

            int read = _decryptedCdnStream.Read(buffer, offset, count);
            if (read > 0)
            {
                // Write to cache file
                //                _cacheFileStream.Write(buffer, offset, read);
                _totalBytesFetchedFromCdn += read;
            }
            else
            {
                FinalizeCacheFile();
            }

            bytesRead += read;
            _position += read;
        }

        return bytesRead;
    }

    private void FinalizeCacheFile()
    {
        if (_decryptedCdnStream.BaseStream is SimplifiedStreamingAudioFileStream streamingStream)
        {
            if (streamingStream.EverythingFetched(out var bytes))
            {
                string cacheFilePath = GetCacheFilePath();
                if (File.Exists(cacheFilePath))
                {
                    File.Delete(cacheFilePath);
                }

                // write to file
                File.WriteAllBytes(cacheFilePath, bytes);
            }
        }
    }

    private long _totalBytesFetchedFromCdn = 0;

    public override long Seek(long offset, SeekOrigin origin)
    {
        lock (_lock)
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
                    newPosition = _length + offset;
                    break;

                default:
                    throw new ArgumentException("Invalid SeekOrigin", nameof(origin));
            }

            if (newPosition < 0 || newPosition > _length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "Attempted to seek outside the stream bounds.");
            }

            _position = newPosition;
            return _position;
        }
    }


    public override bool CanRead => true;
    public override bool CanSeek => _canSeek;
    public override bool CanWrite => false;

    public override long Length => _length;

    public override long Position
    {
        get => _position;
        set => Seek(value, SeekOrigin.Begin);
    }

    public override void Flush()
    {
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException("SetLength not supported.");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException("Write not supported.");
    }
    //
    // private WaveeSampleProvider? _sampleProvider;
    //
    // public override ISampleProviderExtended CreateSampleProvider()
    // {
    //     _sampleProvider = new WaveeSampleProvider(this);
    //     return _sampleProvider;
    // }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                // Cancel the buffering task
                // _bufferingCancellationTokenSource?.Cancel();
                // try
                // {
                //     _bufferingTask?.Wait();
                // }
                // catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is OperationCanceledException))
                // {
                //     // Expected when the task is canceled
                // }

                //_headStream?.Dispose();
                FinalizeCacheFile();
                _decryptedCdnStream?.Dispose();
                // _bufferingCancellationTokenSource?.Dispose();
            }
            catch (Exception)
            {
                // ignore
            }
        }

        base.Dispose(disposing);
    }

    //
    // public sealed class WaveeSampleProvider : ISampleProviderExtended
    // {
    //     private readonly VorbisDecoder _decoder;
    //     private readonly OggReader _oggReader;
    //
    //     public WaveeSampleProvider(Stream waveeAudioStream)
    //     {
    //         waveeAudioStream.Position = 0;
    //         var oggReaderMaybe = OggReader.TryNew(
    //             source: new MediaSourceStream(waveeAudioStream, MediaSourceStreamOptions.Default),
    //             FormatOptions.Default with { EnableGapless = true }
    //         );
    //         var oggReader = oggReaderMaybe.Match(
    //             Succ: x => x,
    //             Fail: e =>
    //             {   
    //                 var error = oggReaderMaybe.Match(
    //                     Succ: _ => throw new Exception("Unexpected success"),
    //                     Fail: e => e
    //                 );
    //                 throw error;
    //             }
    //         );
    //         var track = oggReader.DefaultTrack.ValueUnsafe();
    //         var decoderMaybe = VorbisDecoder.TryNew(track.CodecParams, new DecoderOptions());
    //         var decoder = decoderMaybe.Match(
    //             Succ: x => x,
    //             Fail: e =>
    //             {
    //                 var error = decoderMaybe.Match(
    //                     Succ: _ => throw new Exception("Unexpected success"),
    //                     Fail: e => e
    //                 );
    //                 throw error;
    //             }
    //         );
    //
    //         _oggReader = oggReader;
    //         _decoder = decoder;
    //         ReadNextPacket();
    //     }
    //
    //     private float[] _buffer = [];
    //     private WaveFormat waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
    //
    //     public int Read(float[] buffer, int offset, int count)
    //     {
    //         // we do not know how many samples ReadNextPacket will decode
    //         // so we have to create a loop that will fill the buffer
    //         // teh buffer may still containg left over samples from the previous call
    //         // so consume those first, then read the next packet
    //         // we might still not even have enough samples to fill the buffer so we have to loop
    //         // until we read count samples or we run out of packets
    //         int samplesRead = 0;
    //         while (samplesRead < count)
    //         {
    //             if (_buffer.Length == 0)
    //             {
    //                 ReadNextPacket();
    //             }
    //
    //             if (_buffer.Length == 0)
    //             {
    //                 break;
    //             }
    //
    //             int samplesToCopy = Math.Min(count - samplesRead, _buffer.Length);
    //             Array.Copy(_buffer, 0, buffer, offset + samplesRead, samplesToCopy);
    //             samplesRead += samplesToCopy;
    //
    //             if (samplesToCopy < _buffer.Length)
    //             {
    //                 // we have left over samples
    //                 float[] newBuffer = new float[_buffer.Length - samplesToCopy];
    //                 Array.Copy(_buffer, samplesToCopy, newBuffer, 0, newBuffer.Length);
    //                 _buffer = newBuffer;
    //             }
    //             else
    //             {
    //                 _buffer = Array.Empty<float>();
    //             }
    //         }
    //
    //         return samplesRead;
    //     }
    //
    //     private void ReadNextPacket()
    //     {
    //         var packetMaybe = _oggReader.NextPacket()
    //             .Match(Succ: x => Option<OggPacket>.Some(x), Fail: e =>
    //             {
    //                 Console.WriteLine($"Error reading packet: {e.Message}");
    //                 return Option<OggPacket>.None;
    //             });
    //         if (packetMaybe.IsNone)
    //         {
    //             return;
    //         }
    //
    //         var packet = packetMaybe.ValueUnsafe();
    //         // Decode the packet into audio samples.
    //         var decodeResult = _decoder.Decode(packet);
    //         if (decodeResult.IsFaulted)
    //         {
    //             return;
    //         }
    //
    //         _buffer = decodeResult.Match(x => x, e => throw e);
    //     }
    //
    //     public WaveFormat WaveFormat
    //     {
    //         get => waveFormat;
    //         set => waveFormat = value;
    //     }
    //
    //     public TimeSpan CurrentTime => _oggReader.Position.IfNone(() => { return TimeSpan.Zero; });
    //
    //     public void Seek(TimeSpan startFrom)
    //     {
    //         var sw = Stopwatch.StartNew();
    //         _buffer = Array.Empty<float>();
    //         const int MAX_SEEK_ATTEMPTS = 10;
    //         int attempts = 0;
    //         while (attempts < MAX_SEEK_ATTEMPTS)
    //         {
    //             try
    //             {
    //                 _oggReader.Seek(SeekMode.Accurate, startFrom);
    //                 break;
    //             }
    //             catch (Exception e)
    //             {
    //                 Console.WriteLine($"Error seeking: {e.Message}");
    //                 attempts++;
    //                 // go back a bit and try again
    //                 startFrom = startFrom.Subtract(TimeSpan.FromMilliseconds(100));
    //                 continue;
    //             }
    //         }
    //
    //         // read the next packet to fill the buffer
    //         ReadNextPacket();
    //         var position = _oggReader.Position;
    //
    //         sw.Stop();
    //         Console.WriteLine($"Seek took {sw.ElapsedMilliseconds}ms");
    //     }
    //
    //     public void Clear()
    //     {
    //         _buffer = Array.Empty<float>();
    //     }
    // }
}