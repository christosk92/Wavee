using LanguageExt;
using Spotify.Metadata;
using Wavee.Core.Contracts;
using Wavee.Core.Infrastructure.Traits;
using Wavee.Spotify.Playback.Infrastructure.Sys;

namespace Wavee.Spotify.Playback.Playback.Streams;

internal sealed class SpotifyStream<RT> : Stream, ISpotifyStream where RT : struct, HasHttp<RT>
{
    private readonly Option<NormalisationData> _normalisationDatas;
    private readonly long _offset;
    private DecryptedSpotifyStream<RT> _decryptedStream;

    public SpotifyStream(DecryptedSpotifyStream<RT> decryptedStream,
        Option<NormalisationData> normalisationDatas,
        ulong offset, AudioFile chosenFile,
        Option<string> uid,
        ITrack track)
    {
        Uid = uid;
        _decryptedStream = decryptedStream;
        _normalisationDatas = normalisationDatas;
        ChosenFile = chosenFile;
        Track = track;
        _offset = (long)offset;
    }

    protected override void Dispose(bool disposing)
    {
        _decryptedStream.Dispose();
        _decryptedStream = null!;
        base.Dispose(disposing);
    }

    public AudioFile ChosenFile { get; }
    public Option<string> Uid { get; }

    public ITrack Track { get; }

    public Stream AsStream()
    {
        return this;
    }

    public override void Flush()
    {
        throw new NotImplementedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var span = buffer.AsSpan(offset, count);
        var len = _decryptedStream.Read(span);
        return len;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return _decryptedStream.Seek(offset + _offset, origin);
    }


    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _decryptedStream.Length - _offset;

    public override long Position
    {
        get => _decryptedStream.Position - _offset;
        set => _decryptedStream.Position = value + _offset;
    }
}