using Wavee.Core.Ids;

namespace Wavee.Core.Player.PlaybackStates;

public readonly record struct NonePlaybackState : IWaveePlaybackState
{
    public static IWaveePlaybackState Default = new NonePlaybackState();
    public bool IsPlaying => false;
    public AudioId TrackId => new AudioId();
}