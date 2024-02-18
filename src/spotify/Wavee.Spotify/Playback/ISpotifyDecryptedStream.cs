namespace Wavee.Spotify.Playback;

public interface ISpotifyDecryptedStream : IDisposable
{
    int Seek(long offset, SeekOrigin begin);
    int Read(Span<byte> buffer);
    long Length { get; }
}