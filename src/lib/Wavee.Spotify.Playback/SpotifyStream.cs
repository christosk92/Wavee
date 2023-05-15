using System.Diagnostics;
using AesCtr;
using LanguageExt;
using Wavee.Core.Contracts;
using Wavee.Spotify.Playback.Playback.Streams;

namespace Wavee.Spotify.Playback;

public sealed class SpotifyStream : Stream, IAudioStream
{
    private readonly long _headerOffset;
    private readonly long _totalLength;

    private readonly Stream _decryptedStream;

    public SpotifyStream(
        Stream stream,
        ITrack track,
        Option<NormalisationData> normData,
        long headerOffset,
        long totalLength,
        Option<CrossfadeController> crossfadeController)
    {
        _headerOffset = headerOffset;
        _totalLength = totalLength;
        CrossfadeController = crossfadeController;
        _decryptedStream = stream;
        Track = track;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = _decryptedStream.Read(buffer, offset, count);
        if (read == 0)
        {
            Debugger.Break();
        }
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        Position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, null)
        };

        _decryptedStream.Seek(Position, SeekOrigin.Begin);
        return Position;
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
        throw new NotSupportedException();
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _totalLength - _headerOffset;

    public override long Position
    {
        get => Math.Max(0, _decryptedStream.Position - _headerOffset);
        set => _decryptedStream.Position = Math.Min(value + _headerOffset, Length);
    }

    public ITrack Track { get; }

    public Stream AsStream()
    {
        return this;
    }

    public Option<CrossfadeController> CrossfadeController { get; }
}