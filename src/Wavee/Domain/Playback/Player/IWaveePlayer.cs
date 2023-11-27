namespace Wavee.Domain.Playback.Player;

public interface IWaveePlayer
{
    ValueTask Play(WaveePlaybackList playlist);
    ValueTask Play(IWaveeMediaSource source);

    void Crossfade(TimeSpan crossfadeDuration);
}