using Spotify.Metadata;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Clients.Mercury.Metadata;

namespace Wavee.Spotify.Clients.Playback.Streams;

internal sealed class SpotifyStream<RT> : Stream, ISpotifyStream where RT : struct, HasHttp<RT>
{
    private readonly Option<NormalisationData> _normalisationDatas;
    private readonly long _offset;
    private readonly DecryptedSpotifyStream<RT> _decryptedStream;
    public SpotifyStream(DecryptedSpotifyStream<RT> decryptedStream,
        Option<NormalisationData> normalisationDatas,
        ulong offset, AudioFile chosenFile, 
        TrackOrEpisode metadata)
    {
        _decryptedStream = decryptedStream;
        _normalisationDatas = normalisationDatas;
        ChosenFile = chosenFile;
        Metadata = metadata;
        _offset = (long)offset;
    }

    public AudioFile ChosenFile { get; }

    public TrackOrEpisode Metadata { get;  }

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