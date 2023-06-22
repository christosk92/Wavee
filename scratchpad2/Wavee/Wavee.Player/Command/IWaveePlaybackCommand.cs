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
        Option<RepeatState> RepeatState) => new WaveePlaybackPlayCommand(
        Context: Context,
        Index: Index,
        StartFrom: StartFrom,
        StartPaused: StartPaused,
        Shuffling: Shuffling,
        RepeatState: RepeatState
    );
}

internal record WaveePlaybackPlayCommand(
    WaveeContext Context,
    int Index,
    Option<TimeSpan> StartFrom,
    bool StartPaused,
    Option<bool> Shuffling,
    Option<RepeatState> RepeatState
) : IWaveePlaybackCommand;