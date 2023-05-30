using LanguageExt;
using Wavee.Core.Ids;
using Wavee.Core.Playback;

namespace Wavee.Core.Player.PlaybackStates;

public readonly record struct WaveePlaybackLoadingState(
    Task<IAudioStream> Stream,
    AudioId TrackId,
    int IndexInContext,
    bool StartPaused,
    Option<TimeSpan> StartAt,
    bool FromQueue) : IWaveePlaybackState
{
    public bool IsPlaying => true;
}