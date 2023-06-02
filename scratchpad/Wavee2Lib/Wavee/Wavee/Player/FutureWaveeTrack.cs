using LanguageExt;
using Wavee.Core.Ids;

namespace Wavee.Player;

public record FutureWaveeTrack(AudioId TrackId, string TrackUid, 
    Func<Task<WaveeTrack>> Factory);