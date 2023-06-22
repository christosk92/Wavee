using LanguageExt;

namespace Wavee.Player.Decoding;

public interface IAudioDecoder : IDisposable
{
    int SampleSize { get; }
    bool IsMarkedForCrossfadeOut { get; }
    TimeSpan CurrentTime { get; }
    TimeSpan TotalTime { get; }
    int Read(Span<float> buffer);

    Unit MarkForCrossfadeOut(TimeSpan duration);
    Unit MarkForCrossfadeIn(TimeSpan duration);
}