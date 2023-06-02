using System.Collections;
using LanguageExt;

namespace Wavee.Player;

public readonly record struct WaveeContext(
    string Id,
    string Name,
    IEnumerable<FutureWaveeTrack> FutureTracks,
    Option<IShuffleProvider> ShuffleProvider
);
public interface IShuffleProvider
{
    int GetNextIndex(int currentIndex, int maxIndex);
}