using LanguageExt;
using Wavee.Core.Contracts;

namespace Wavee.Player.States;

public readonly record struct WaveePlayingState(
    ITrack Track,
    TimeSpan Position,
    Option<int> IndexInContext,
    bool FromQueue
) : IWaveeInPlaybackState
{
    public required IAudioStream Stream { get; init; }

    public WaveeEndedState ToEndedState()
    {
        return new WaveeEndedState(Track, Position, IndexInContext, FromQueue)
        {
            Stream = Stream
        };
    }
}