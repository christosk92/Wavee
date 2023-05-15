using LanguageExt;
using Wavee.Core.Contracts;
using Wavee.Core.Id;

namespace Wavee.Player.States;

public readonly record struct WaveeLoadingState(
    Option<int> IndexInContext,
    Option<AudioId> TrackId,
    bool FromQueue,
    TimeSpan StartFrom,
    bool StartPaused,
    bool CloseOtherStreams
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

    public IWaveeInPlaybackState ToPlayingOrPaused(IAudioStream stream)
    {
        return StartPaused
            ? new WaveePausedState(
                stream.Track,
                StartFrom,
                IndexInContext,
                FromQueue)
            {
                Stream = stream
            }
            : new WaveePlayingState(
                DateTimeOffset.UtcNow,
                StartFrom,
                stream.Track,
                IndexInContext,
                FromQueue)
            {
                Stream = stream,
                Uid = Option<string>.None
            };
    }
}