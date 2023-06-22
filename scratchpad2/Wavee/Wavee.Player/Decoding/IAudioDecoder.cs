namespace Wavee.Player.Decoding;

public interface IAudioDecoder : IDisposable
{
    int SampleSize { get; }
    int Read(Span<float> buffer);
}