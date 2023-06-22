using LanguageExt;
using Wavee.Player.Ctx;

namespace Wavee.Player.Command;

internal interface IWaveePlaybackCommand
{
}

internal record WaveePlaybackPlayCommand(
    WaveeContext Context,
    int Index,
    Option<TimeSpan> StartFrom,
    bool StartPaused,
    Option<bool> Shuffling,
    Option<RepeatState> RepeatState,
    Ref<Option<TimeSpan>> CrossfadeDuration
) : IWaveePlaybackCommand;

internal record WaveePlaybackSkipNextCommand(
    bool CrossfadeIn,
    Ref<Option<TimeSpan>> CrossfadeDuration
) : IWaveePlaybackCommand;