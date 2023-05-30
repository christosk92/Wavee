using System.Diagnostics;
using LanguageExt;
using Spotify.Metadata;
using Wavee.Core.Contracts;
using Wavee.Core.Playback;
using Wavee.Spotify.Models.Response;

namespace Wavee.Spotify.Infrastructure.Playback.Streams;

public sealed class SpotifyStream : Stream, IAudioStream
{
    private readonly long _headerOffset;
    private readonly long _totalLength;

    private readonly Stream _decryptedStream;

    public SpotifyStream(
        Stream stream,
        TrackOrEpisode track,
        HashMap<string, string> metadata,
        Option<NormalisationData> normData,
        long headerOffset,
        long totalLength,
        Option<CrossfadeController> crossfadeController)
    {
        _headerOffset = headerOffset;
        _totalLength = totalLength;
        CrossfadeController = crossfadeController;
        Metadata = metadata;
        _decryptedStream = stream;
        Track = track.Value
            .Match(
                Left: x => FromEpisode(x),
                Right: x => FromTrack(x.Value)
            );
        Position = 0;
    }

    private ITrack FromEpisode(Episode episode)
    {
        throw new NotImplementedException();
    }

    private ITrack FromTrack(Track track)
    {
        var country = Metadata["country"];
        var cdnUrl = Metadata["cdnurl"];
        return SpotifyTrackResponse.From(country, cdnUrl, track);
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

        _decryptedStream.Seek(offset + _headerOffset, SeekOrigin.Begin);
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
    public HashMap<string, string> Metadata { get; }

    public Stream AsStream()
    {
        return this;
    }

    protected override void Dispose(bool disposing)
    {
        _decryptedStream.Dispose();
        base.Dispose(disposing);
    }

    public Option<CrossfadeController> CrossfadeController { get; }
}