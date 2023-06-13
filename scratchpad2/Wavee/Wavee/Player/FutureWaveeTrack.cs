using LanguageExt;
using Wavee.Core.Ids;

namespace Wavee.Player;

public record FutureWaveeTrack(AudioId TrackId, string TrackUid, 
    Func<CancellationToken, Task<WaveeTrack>> Factory);