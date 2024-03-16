using System.Diagnostics;

namespace Wavee.Core;

public sealed record WaveePlaybackState
{
    public bool EndOfContextReached { get; set; }
    public IWaveePlayContext Context { get; set; }
    public WaveeMediaSource? Source { get; set; }
    public bool IsActive { get; set; }
    public bool IsBuffering { get; set; }
    public bool Paused { get; set; }
    public bool ShuffleState { get; set; }
    public RepeatState RepeatState { get; set; }
    public Stopwatch PositionStopwatch { get; set; }
    public TimeSpan PositionSinceStartStopwatch { get; set; }
    public int? IndexInContext { get; set; }
}