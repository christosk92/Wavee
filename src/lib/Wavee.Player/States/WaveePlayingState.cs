using LanguageExt;
using Wavee.Core.Contracts;
using Wavee.Core.Id;

namespace Wavee.Player.States;

public readonly record struct WaveePlayingState(
    DateTimeOffset Since,
    TimeSpan PositionAsOfSince,
    ITrack Track,
    Option<int> IndexInContext,
    Option<string> Uid,
    bool FromQueue
) : IWaveeInPlaybackState
{
    public TimeSpan Position => (DateTimeOffset.UtcNow - Since) + PositionAsOfSince;
    public required IAudioStream Stream { get; init; }

    public WaveePausedState ToPausedState(TimeSpan position)
    {
        return new WaveePausedState(Track, position, IndexInContext, Uid, FromQueue)
        {
            Stream = Stream
        };
    }

    public WaveeEndedState ToEndedState()
    {
        return new WaveeEndedState(Track, Position, IndexInContext, Uid, FromQueue)
        {
            Stream = Stream
        };
    }

    public Option<AudioId> TrackId => Track.Id;
    public required Option<string> Uid { get; init; }
}