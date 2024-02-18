using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Wavee.Core;

public sealed class WaveePlayer : IWaveePlayer
{
    private readonly Subject<WaveePlaybackState> _events = new();
    private WaveePlaybackState _state;

    public WaveePlayer()
    {
        _state = new WaveePlaybackState
        {
            PositionSinceStartStopwatch = TimeSpan.Zero,
            PositionStopwatch = new Stopwatch()
        };
    }

    public IObservable<WaveePlaybackState> Events => _events.StartWith(_state);

    public async Task Play(IWaveePlayContext spotifyPlayContext, int startAt, CancellationToken cancel)
    {
        var source = await spotifyPlayContext.GetAt(startAt, cancel);
        if (source is null) return;
        _state = new WaveePlaybackState
        {
            IsActive = true,
            Source = source,
            PositionSinceStartStopwatch = TimeSpan.Zero,
            PositionStopwatch = Stopwatch.StartNew()
        };
        _events.OnNext(_state);
    }
}

public interface IWaveePlayer
{
    IObservable<WaveePlaybackState> Events { get; }
    Task Play(IWaveePlayContext spotifyPlayContext, int startAt, CancellationToken cancel);
}