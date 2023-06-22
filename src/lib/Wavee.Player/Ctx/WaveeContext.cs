namespace Wavee.Player.Ctx;

public readonly record struct WaveeContext(
    string Id,
    string Name,
    IEnumerable<FutureWaveeTrack> FutureTracks,
    IShuffleProvider ShuffleProvider
);

public interface IShuffleProvider
{
    int GetNextIndex(int currentIndex);
}

public class RandomShuffleProvider : IShuffleProvider
{
    private readonly Random _random;

    private static Random SharedRandom { get; } = new();
    public RandomShuffleProvider()
    {
        _random = SharedRandom;
    }

    public int GetNextIndex(int currentIndex)
    {
        return _random.Next();
    }
}