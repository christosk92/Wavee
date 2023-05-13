namespace Wavee.Player.States;

public readonly record struct WaveeNothingState : IWaveePlaybackState
{
    public static WaveeNothingState Default = new WaveeNothingState();
}