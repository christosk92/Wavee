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
    public Task Play(IWaveePlayContext spotifyPlayContext, int startAt, CancellationToken cancel)
    {
        throw new NotImplementedException();
    }
}

public interface IWaveePlayer
{
    IObservable<WaveePlaybackState> Events { get; }
    Task Play(IWaveePlayContext spotifyPlayContext, int startAt, CancellationToken cancel);
}