namespace Wavee.Spotify.Playback;

public sealed class CachedSpotifyEncryptedStream : SpotifyEncryptedStream
{
    private readonly FileStream _fileStream;

    public CachedSpotifyEncryptedStream(FileStream fileStream) : base(fileStream.Length)
    {
        _fileStream = fileStream;
    }

    public override bool IsCached => true;

    public override Task<byte[]> GetChunk(int chunkIndex, CancellationToken cancel)
    {
        throw new NotImplementedException();
    }

    public override void Dispose()
    {
        base.Dispose();

        _fileStream.Dispose();
    }
}