using LanguageExt;
using Wavee.Core.Contracts;

namespace Wavee.Player.States;

public readonly record struct WaveePlayingState(
    DateTimeOffset Since,
    TimeSpan PositionAsOfSince,
    ITrack Track,
    Option<int> IndexInContext,
    bool FromQueue
) : IWaveeInPlaybackState
{
    public TimeSpan Position => (DateTimeOffset.UtcNow - Since) + PositionAsOfSince;
    public required IAudioStream Stream { get; init; }
    public WaveePausedState ToPausedState(TimeSpan position)
    {
        return new WaveePausedState(Track, position, IndexInContext, FromQueue)
        {
            Stream = Stream
        };
    }
    public WaveeEndedState ToEndedState()
    {
        return new WaveeEndedState(Track, Position, IndexInContext, FromQueue)
        {
            Stream = Stream
        };
    }
}