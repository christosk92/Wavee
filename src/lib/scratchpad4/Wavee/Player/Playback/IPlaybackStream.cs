namespace Wavee.Player.Playback;

public interface IPlaybackStream
{
    IPlaybackItem Item { get; }
    Stream AsStream();
}