using LanguageExt;
using Wavee.Core.Ids;

namespace Wavee.Core.Player.PlaybackStates;

public interface IWaveePlaybackState
{
    bool IsPlaying { get; }
    AudioId TrackId { get; }
}