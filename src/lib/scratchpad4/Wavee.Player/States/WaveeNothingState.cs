using LanguageExt;
using Wavee.Core.Id;

namespace Wavee.Player.States;

public readonly record struct WaveeNothingState : IWaveePlaybackState
{
    public static WaveeNothingState Default = new WaveeNothingState();
    public Option<AudioId> TrackId => Option<AudioId>.None;
}