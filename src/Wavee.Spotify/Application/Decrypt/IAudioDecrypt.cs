namespace Wavee.Infrastructure.Playback.Decrypt;

public interface IAudioDecrypt
{
    void Decrypt(int chunkIndex, byte[] buffer);
}