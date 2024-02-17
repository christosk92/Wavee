using System.Diagnostics;

namespace Wavee.Core;

public sealed class WaveePlaybackState
{
    public IWaveeMediaSource Source { get; set; }
    public bool IsActive { get; set; }
    public bool Paused { get; set; }
    public bool ShuffleState { get; set; }
    public RepeatState RepeatState { get; set; }
    
    public Stopwatch PositionStopwatch { get; set; }
    public TimeSpan PositionSinceStartStopwatch { get; set; }
}