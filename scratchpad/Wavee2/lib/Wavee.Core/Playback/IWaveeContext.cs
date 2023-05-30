using LanguageExt;
using Wavee.Core.Ids;

namespace Wavee.Core.Playback;

public readonly record struct WaveeContext(
    string Id,
    string Name,
    IEnumerable<FutureTrack> FutureTracks,
    Option<IShuffleProvider> ShuffleProvider
);

public readonly record struct FutureTrack(
    AudioId Id,
    HashMap<string, string> Metadata,
    Func<Task<IAudioStream>> StreamFuture);