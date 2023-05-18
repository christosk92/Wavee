using Wavee.Core.Ids;

namespace Wavee.Core.Player.PlaybackStates;

public readonly record struct PermanentEndOfContextPlaybackState : IWaveePlaybackState
{
    public static PermanentEndOfContextPlaybackState Default = new PermanentEndOfContextPlaybackState();
    public bool IsPlaying => false;
    public AudioId TrackId { get; }
}