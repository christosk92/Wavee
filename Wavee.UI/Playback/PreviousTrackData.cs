namespace Wavee.UI.Playback;

public readonly record struct PreviousTrackData(string Id, DateTime StartedAt, TimeSpan RealDuration);
