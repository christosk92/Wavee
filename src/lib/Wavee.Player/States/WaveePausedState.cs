using LanguageExt;
using Wavee.Core.Contracts;
using Wavee.Core.Id;

namespace Wavee.Player.States;

public readonly record struct WaveePausedState(
    ITrack Track,
    TimeSpan Position,
    Option<int> IndexInContext,
    Option<string> Uid,
    bool FromQueue
) : IWaveeInPlaybackState
{
    public required IAudioStream Stream { get; init; }

    public WaveePlayingState ToPlayingState()
    {
        return new WaveePlayingState(
            DateTimeOffset.UtcNow,
            Position,
            Track, IndexInContext,
            Uid,
            FromQueue)
        {
            Uid = Option<string>.None,
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
}