using LanguageExt;
using Wavee.Core.Contracts;

namespace Wavee.Player.States;

public readonly record struct WaveeEndedState(ITrack Track,
    TimeSpan Position,
    Option<int> IndexInContext,
    bool FromQueue
) : IWaveeInPlaybackState
{
    public required IAudioStream Stream { get; init; }

    public WaveePlayingState ToPlayingState()
    {
        return new WaveePlayingState(
            DateTimeOffset.UtcNow,
            Position,
            Track, IndexInContext, FromQueue)
        {
            Stream = Stream
        };
    }
}