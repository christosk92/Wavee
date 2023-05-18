namespace Wavee.Core.Playback;

public interface IShuffleProvider
{
    int GetNextIndex(int currentIndex, int maxIndex);
}