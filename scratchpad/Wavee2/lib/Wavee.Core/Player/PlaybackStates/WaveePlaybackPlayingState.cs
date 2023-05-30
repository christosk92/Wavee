using Wavee.Core.Ids;
using Wavee.Core.Playback;

namespace Wavee.Core.Player.PlaybackStates;

public readonly record struct WaveePlaybackPlayingState(
        IAudioDecoder Decoder,
        IAudioStream Stream,
        AudioId TrackId,
        int IndexInContext,
        bool FromQueue,
        bool Paused
    )
    : IWaveePlaybackState
{
    public bool IsPlaying => true;
}