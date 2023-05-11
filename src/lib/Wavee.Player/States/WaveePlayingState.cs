using NAudio.Wave;

namespace Wavee.Player.States;

public readonly record struct WaveePlayingState(string PlaybackId, WaveStream Decoder) : IWaveePlayerInPlaybackState
{
    public TimeSpan Position => Decoder.CurrentTime;

    public WaveePausedState ToPaused()
    {
        return new WaveePausedState(PlaybackId, Decoder);
    }
}

public readonly record struct WaveePausedState(string PlaybackId, WaveStream Decoder) : IWaveePlayerInPlaybackState
{
    public TimeSpan Position => Decoder.CurrentTime;
}

public readonly record struct WaveeEndOfTrackState(
    string PlaybackId, WaveStream Decoder,
    bool GoingToNextTrackAlready) : IWaveePlayerInPlaybackState
{
    public DateTimeOffset Since { get; } = new DateTimeOffset();
    public TimeSpan Position => Decoder.CurrentTime;
}