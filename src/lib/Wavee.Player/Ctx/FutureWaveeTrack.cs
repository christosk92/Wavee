namespace Wavee.Player.Ctx;
public record FutureWaveeTrack(string TrackId, string TrackUid, Func<CancellationToken, Task<WaveeTrack>> Factory);