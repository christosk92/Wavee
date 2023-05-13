using LanguageExt;
using Wavee.Core.Id;

namespace Wavee.Player.States;

public readonly record struct WaveeLoadingState(
    Option<int> IndexInContext,
    Option<AudioId> TrackId,
    bool FromQueue,
    TimeSpan StartFrom,
    bool StartPaused
) : IWaveePlaybackState
{
    public required Task<IAudioStream> Stream { get; init; }

    public WaveeEndedState ToEndedState()
    {
        var stream = Stream.ConfigureAwait(false).GetAwaiter().GetResult();
        return new WaveeEndedState(
            stream.Track,
            stream.Track.Duration,
            IndexInContext,
            FromQueue)
        {
            Stream = stream
        };
    }
}