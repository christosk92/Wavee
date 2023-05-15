namespace Wavee.Core.Contracts;

public interface IAudioStream
{
    ITrack Track { get; }
    Stream AsStream();

    Option<CrossfadeController> CrossfadeController { get; }
}

public class CrossfadeController
{
    private bool _crossfadeOutStarted;
    private bool _crossfadeStarted;
    private readonly TimeSpan _crossfadeDuration;

    public CrossfadeController(TimeSpan crossfadeDuration)
    {
        _crossfadeDuration = crossfadeDuration;
    }

    public TimeSpan Duration => _crossfadeDuration;

    // public float GetFadeIn(TimeSpan position)
    // {
    //     if (!_crossfadeStarted) return 1;
    //     // 0 -> 1
    //     if (position.TotalMilliseconds < _crossfadeDuration.TotalMilliseconds)
    //     {
    //         return (float)(position.TotalMilliseconds / _crossfadeDuration.TotalMilliseconds);
    //     }
    //
    //     return 1;
    // }
    //
    // public float GetFadeOut(TimeSpan position, TimeSpan duration)
    // {
    //     if (!_crossfadeStarted) return 1;
    //     // 1 -> 0
    //     if (position.TotalMilliseconds > duration.TotalMilliseconds - _crossfadeDuration.TotalMilliseconds)
    //     {
    //         return (float)((duration.TotalMilliseconds - position.TotalMilliseconds) /
    //                        _crossfadeDuration.TotalMilliseconds);
    //     }
    //
    //     return 1;
    // }

    public float GetFactor(TimeSpan position, TimeSpan trackDuration)
    {
        if (_crossfadeStarted)
        {
            // 0 -> 1
            if (position.TotalMilliseconds < _crossfadeDuration.TotalMilliseconds)
            {
                return (float)(position.TotalMilliseconds / _crossfadeDuration.TotalMilliseconds);
            }
        }

        if (_crossfadeOutStarted)
        {
            // 1 -> 0
            if (position.TotalMilliseconds > trackDuration.TotalMilliseconds - _crossfadeDuration.TotalMilliseconds)
            {
                return (float)((trackDuration.TotalMilliseconds - position.TotalMilliseconds) /
                               _crossfadeDuration.TotalMilliseconds);
            }
        }

        return 1f;
    }

    public bool MaybeFlagCrossFadeOut(TimeSpan position, TimeSpan trackDuration)
    {
        if (!_crossfadeOutStarted && position.TotalMilliseconds > trackDuration.TotalMilliseconds - _crossfadeDuration.TotalMilliseconds)
        {
            _crossfadeOutStarted = true;
            return true;
        }

        return false;
    }

    public void FlagCrossFadeIn()
    {
        _crossfadeStarted = true;
    }
}