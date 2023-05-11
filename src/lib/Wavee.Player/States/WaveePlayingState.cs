namespace Wavee.Player.States;

public readonly record struct WaveePlayingState(string PlaybackId,
    Stream Decoder) : IWaveePlayerInPlaybackState
{
    public required DateTimeOffset Timestamp { get; init; }
    public required TimeSpan PositionAsOfTimestamp { get; init; }

    public TimeSpan Position
    {
        get
        {
            var now = DateTimeOffset.Now;
            var elapsed = now - Timestamp;
            return PositionAsOfTimestamp + elapsed;
        }
    }

    public WaveePausedState ToPaused()
    {
        return new WaveePausedState(PlaybackId, Decoder)
        {
            Position = Position
        };
    }
}

public readonly record struct WaveePausedState(string PlaybackId, Stream Decoder) : IWaveePlayerInPlaybackState
{
    public required TimeSpan Position { get; init; }
}

public readonly record struct WaveeEndOfTrackState(
    string PlaybackId, Stream Decoder,
    bool GoingToNextTrackAlready) : IWaveePlayerInPlaybackState
{
    public DateTimeOffset Since { get; } = new DateTimeOffset();
    public required TimeSpan Position { get; init; }
}