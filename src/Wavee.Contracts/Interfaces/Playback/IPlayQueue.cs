namespace Wavee.Contracts.Interfaces.Playback;

public interface IPlayQueue
{
    bool HasNext();
    IMediaSource? NextTrack(bool shuffle);
    bool HasPrevious();
    IMediaSource? PreviousTrack();
}