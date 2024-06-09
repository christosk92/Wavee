using System.Diagnostics;

namespace Wavee.Contracts.Common;

public readonly record struct RealTimePosition
{
    private readonly TimeSpan _position;
    private readonly Stopwatch _stopwatch;

    private RealTimePosition(TimeSpan position, Stopwatch stopwatch)
    {
        _position = position;
        _stopwatch = stopwatch;
    }

    public TimeSpan Value => _position + _stopwatch.Elapsed;

    public static RealTimePosition Create(TimeSpan position, Stopwatch stopwatch)
    {
        return new RealTimePosition(position, stopwatch);
    }
}