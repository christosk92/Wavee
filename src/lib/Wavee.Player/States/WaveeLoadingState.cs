using LanguageExt;
using Wavee.Core;
using Wavee.Core.Contracts;
using Wavee.Core.Id;

namespace Wavee.Player.States;

public readonly record struct WaveeLoadingState(
    Option<int> IndexInContext,
    Option<AudioId> TrackId,
    Option<string> Uid,
    bool FromQueue,
    TimeSpan StartFrom,
    bool StartPaused,
    bool StartFadeIn
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
            Uid,
            FromQueue)
        {
            Stream = stream
        };
    }

    public IWaveeInPlaybackState ToPlayingOrPaused(IAudioStream stream, IAudioDecoder audioDecoder)
    {
        return StartPaused
            ? new WaveePausedState(
                stream.Track,
                StartFrom,
                IndexInContext,
                Uid,
                FromQueue)
            {
                Stream = stream,
                Decoder = audioDecoder
            }
            : new WaveePlayingState(
                DateTimeOffset.UtcNow,
                StartFrom,
                stream.Track,
                IndexInContext,
                Uid,
                FromQueue)
            {
                Stream = stream,
                Uid = Option<string>.None,
                Decoder = audioDecoder
            };
    }
}