namespace Wavee.Player;

public interface IShuffleProvider
{
    int GetNextIndex(int currentIndex, int maxIndex);
}