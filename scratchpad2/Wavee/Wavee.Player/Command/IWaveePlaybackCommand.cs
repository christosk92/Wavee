using LanguageExt;
using Wavee.Player.Ctx;

namespace Wavee.Player.Command;

internal interface IWaveePlaybackCommand
{
    static IWaveePlaybackCommand Play(WaveeContext Context,
        int Index,
        Option<TimeSpan> StartFrom,
        bool StartPaused,
        Option<bool> Shuffling,
        Option<RepeatState> RepeatState,
        Ref<Option<TimeSpan>> CrossfadeDuration) => new WaveePlaybackPlayCommand(
        Context: Context,
        Index: Index,
        StartFrom: StartFrom,
        StartPaused: StartPaused,
        Shuffling: Shuffling,
        RepeatState: RepeatState,
        CrossfadeDuration: CrossfadeDuration
    );
    
    static WaveePlaybackSkipNextCommand SkipNext(bool crossfadeIn, Ref<Option<TimeSpan>> crossfadeDuration) =>
        new WaveePlaybackSkipNextCommand(
            CrossfadeIn: crossfadeIn,
            CrossfadeDuration: crossfadeDuration
        );
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