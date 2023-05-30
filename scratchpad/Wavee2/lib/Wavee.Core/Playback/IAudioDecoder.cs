namespace Wavee.Core.Playback;

public interface IAudioDecoder : IDisposable
{
    TimeSpan DecodePosition { get; }
    bool Ended { get; }
    void ReadSamples(float[] buffer);
    IAudioDecoder Seek(TimeSpan to);
}