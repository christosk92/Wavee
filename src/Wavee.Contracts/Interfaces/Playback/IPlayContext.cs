namespace Wavee.Contracts.Interfaces.Playback;

public interface IPlayContext : IPlayQueue
{
    void ResetToFirst();
    void ResetToLast();
}