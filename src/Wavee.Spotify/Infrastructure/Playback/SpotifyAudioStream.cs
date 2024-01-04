using Wavee.Interfaces;
using Wavee.Interfaces.Models;
using Wavee.Spotify.Core.Clients.Playback;
using Wavee.Spotify.Core.Models.Track;
using Wavee.Spotify.Decrypt;
using Wavee.Spotify.Interfaces.Clients.Playback;
using Wavee.Spotify.Interfaces.Models;

namespace Wavee.Spotify.Infrastructure.Playback;

public abstract class SpotifyAudioStream : Stream, IWaveeMediaSource
{
    private long _pos;
    internal const int ChunkSize = 2 * 2 * 128 * 1024;
    private readonly Dictionary<int, byte[]> _decryptedChunks = new();
    private readonly IAudioDecrypt? _decrypt;
    private Exception? _lastException;

    private static byte[] AUDIO_AES_IV = new byte[]
    {
        0x72, 0xe0, 0x67, 0xfb, 0xdd, 0xcb, 0xcf, 0x77, 0xeb, 0xe8, 0xbc, 0x64, 0x3f, 0x63, 0x0d, 0x93,
    };

    internal SpotifyAudioStream(SpotifyAudioKey audioKey, TimeSpan duration, IWaveePlayableItem item)
    {
        Duration = duration;
        Item = item;
        if (audioKey.HasKey)
        {
            _decrypt = new BouncyCastleDecrypt(audioKey.Key!, AUDIO_AES_IV, ChunkSize);
        }
    }


    public NormalisationData? ReadNormalisationData()
    {
        var data = Wavee.Spotify.Core.Clients.Playback.NormalisationData.ParseFromOgg(this);
        return data;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        TaskCompletionSource? tcs = null;

        try
        {
            var chunkIndex = (int)(_pos / ChunkSize);
            var chunkOffset = (int)(_pos % ChunkSize);

            if (!_decryptedChunks.TryGetValue(chunkIndex, out var chunk))
            {
                tcs = new TaskCompletionSource();
                BufferingStream?.Invoke(this, tcs);
                chunk = GetChunk(chunkIndex,
                        false,
                        CancellationToken.None)
                    .ToArray(); //create a copy
                if (_decrypt != null)
                {
                    _decrypt.Decrypt(chunkIndex, chunk);
                }

                _decryptedChunks[chunkIndex] = chunk;
                tcs.SetResult();
            }

            var bytesToRead = Math.Max(0, Math.Min(count, chunk.Length - chunkOffset));
            Array.Copy(chunk, chunkOffset, buffer, offset, bytesToRead);
            _pos += bytesToRead;
            if (_lastException != null)
            {
                ResumedFromError?.Invoke(this, _lastException);
                _lastException = null;
            }

            return bytesToRead;
        }
        catch (Exception requestException)
        {
            tcs?.SetException(requestException);
            _lastException = requestException;
            OnError?.Invoke(this, requestException);
            // Wait 2 seconds before trying again?
            Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false).GetAwaiter().GetResult();
            throw;
        }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var to = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, null)
        };

        Position = to;
        return Position;
    }

    public long SeekWithoutOffset(long offset, SeekOrigin origin)
    {
        var to = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, null)
        };
        _pos = to;
        return _pos;
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => AudioFileSize - Offset;

    public override long Position
    {
        get => Math.Max(0, _pos - Offset);
        set
        {
            var to = Math.Min(AudioFileSize, value + Offset);
            _pos = to;
        }
    }

    public abstract long Offset { get; }
    public abstract long AudioFileSize { get; }

    public abstract byte[] GetChunk(int chunkIndex, bool preloading, CancellationToken cancellationToken);

    protected override void Dispose(bool disposing)
    {
        _decryptedChunks.Clear();
        base.Dispose(disposing);
    }

    public ISpotifyPlayableItem AsSpotifyItem() => Item as ISpotifyPlayableItem ?? throw new InvalidOperationException();
    public IWaveePlayableItem Item { get; }
    public Stream AsStream() => this;

    public TimeSpan Duration { get; }
    public event EventHandler<TaskCompletionSource>? BufferingStream;
    public event EventHandler<Exception> OnError;
    public event EventHandler<Exception>? ResumedFromError;
}