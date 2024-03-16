using Wavee.Core;
using Wavee.Spotify.Extensions;
using Wavee.Spotify.Models.Interfaces;

namespace Wavee.Spotify.Playback;

public class SpotifyMediaSource : WaveeMediaSource
{
    private readonly ISpotifyDecryptedStream _stream;
    private readonly int _offset;
    private long _pos;

    public SpotifyMediaSource(ISpotifyDecryptedStream stream,
        ISpotifyPlayableItem item,
        int offset,
        NormalisationData? normalisationData) : base(stream.Length - offset, item.Duration,item)
    {
        _stream = stream;
        Item = item;
        _offset = offset;
        NormalisationData = normalisationData;
    }

    public ISpotifyPlayableItem Item { get; }
    public NormalisationData? NormalisationData { get; }

    public override int Read(byte[] buffer, int offset, int count)
    {
        // Ensure the buffer is not null and the arguments are valid
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (offset < 0 || offset > buffer.Length) throw new ArgumentOutOfRangeException(nameof(offset));
        if (count < 0 || offset + count > buffer.Length) throw new ArgumentOutOfRangeException(nameof(count));

        // Create a Span<byte> over the portion of the array we're interested in.
        Span<byte> span = new Span<byte>(buffer, offset, count);
        var read = _stream.Read(span);
        _pos += read;
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var to = origin switch
        {
            SeekOrigin.Begin => offset + _offset,
            SeekOrigin.Current => _pos + offset + _offset,
            SeekOrigin.End => Length - offset + _offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, null)
        };
        _pos = to - _offset;
        return _stream.Seek(to, SeekOrigin.Begin);
    }

    public override long Position
    {
        get => _pos;
        set => Seek(value, SeekOrigin.Begin);
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }
}