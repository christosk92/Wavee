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

    public static IShuffleProvider Default { get; } = new RandomShuffleProvider();
}

internal class RandomShuffleProvider : IShuffleProvider
{
    private readonly Random _random;

    public RandomShuffleProvider()
    {
        _random = Random.Shared;
    }

    public int GetNextIndex(int currentIndex)
    {
        return _random.Next();
    }
}