using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Playback.Cdn;
using Wavee.Spotify.Playback.Infrastructure.Sys;

namespace Wavee.Spotify.Playback.Infrastructure.Streams;

internal sealed class EncryptedSpotifyStream<RT> : Stream where RT : struct, HasHttp<RT>
{
    private readonly MaybeExpiringUrl _cdnUrl;
    private readonly TrackOrEpisode _metadata;
    private readonly ReadOnlyMemory<byte> _firstChunk;
    private readonly int _numberOfChunks;

    public EncryptedSpotifyStream(
        MaybeExpiringUrl cdnUrl,
        TrackOrEpisode metadata,
        ReadOnlyMemory<byte> firstChunk, int numberOfChunks, long length)
    {
        _cdnUrl = cdnUrl;
        _metadata = metadata;
        _firstChunk = firstChunk;
        _numberOfChunks = numberOfChunks;
        Length = length;
    }

    public override long Position { get; set; }
    public override bool CanWrite => false;
    public override long Length { get; }

    public override void Flush()
    {
        throw new NotImplementedException();
    }


    public override int Read(byte[] buffer, int offset, int count)
    {
        var chunkToRead = (int)(Position / SpotifyPlaybackRuntime.ChunkSize);
        var chunkOffset = (int)(Position % SpotifyPlaybackRuntime.ChunkSize);

        if (chunkToRead == 0)
        {
            var firstChunkToRead = _firstChunk.Slice(chunkOffset, Math.Min(count, _firstChunk.Length - chunkOffset));
            firstChunkToRead.CopyTo(buffer.AsMemory(offset, firstChunkToRead.Length));
            Position += firstChunkToRead.Length;
            return firstChunkToRead.Length;
        }

        return 0;
    }


    public override long Seek(long to, SeekOrigin begin)
    {
        switch (begin)
        {
            case SeekOrigin.Begin:
                Position = to;
                break;
            case SeekOrigin.Current:
                Position += to;
                break;
            case SeekOrigin.End:
                Position = Length - to;
                break;
        }

        return Position;
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
    
}