using LanguageExt;

namespace Wavee.Player.Decoding;

public interface IAudioDecoder : IDisposable
{
    void Pause();
    void Resume();

    bool IsMarkedForCrossfadeOut { get; }
    TimeSpan CurrentTime { get; }
    TimeSpan TotalTime { get; }
    IObservable<TimeSpan> TimeChanged { get; }
    IObservable<Unit> TrackEnded { get; }
    Unit MarkForCrossfadeOut(TimeSpan duration);
    Unit MarkForCrossfadeIn(TimeSpan duration);
}