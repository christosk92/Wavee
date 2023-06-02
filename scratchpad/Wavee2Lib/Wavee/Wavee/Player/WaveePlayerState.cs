using LanguageExt;
using Wavee.Core.Ids;

namespace Wavee.Player;

public readonly record struct WaveePlayerState(
    Option<AudioId> TrackId,
    Option<string> TrackUid,
    bool IsPaused,
    bool IsShuffling,
    RepeatState RepeatState,
    Option<IWaveeContext> Context,
    Option<WaveeTrack> TrackDetails)
{
    public static WaveePlayerState Empty()
    {
        return new WaveePlayerState(
            TrackId: Option<AudioId>.None,
            TrackUid: Option<string>.None,
            IsPaused: false,
            IsShuffling: false,
            RepeatState: RepeatState.None,
            Context: Option<IWaveeContext>.None,
            TrackDetails: Option<WaveeTrack>.None
        );
    }
}