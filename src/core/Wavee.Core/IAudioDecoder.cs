namespace Wavee.Core;

public interface IAudioDecoder : IDisposable
{
    int Channels { get; }
    int SampleRate { get; }
    bool IsEndOfStream { get; }
    TimeSpan Position { get; }
    TimeSpan TotalDuration { get; }
    int ReadSamples(Span<float> samples);
    void Seek(TimeSpan position);
}