using Wavee.Core.Ids;
using Wavee.Core.Playback;

namespace Wavee.Core.Player.PlaybackStates;

public readonly record struct WaveePlaybackEndedState(
    IAudioDecoder Decoder,
    IAudioStream Stream,
    bool IsPlaying,
    AudioId TrackId,
    int IndexInContext,
    bool CrossfadingIntoNextTrack
) : IWaveePlaybackState;