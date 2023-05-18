using System.Diagnostics;
using System.Reactive.Linq;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using ReactiveUI;
using Wavee.Core.Contracts;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.Infrastructure.Traits;

namespace Wavee.UI.ViewModels;

public sealed class PlaybackViewModel<R> : ReactiveObject where R : struct, HasSpotify<R>
{
    private const uint TIMER_INTERVAL_MS = 50; // 50 MS

    private readonly Dictionary<Guid, PositionCallbackRecord> _positionCallbacks = new();

    private long _positionMs;
    private readonly Timer _positionTimer;
    private readonly R _runtime;
    private ITrack? _currentTrack;

    public PlaybackViewModel(R runtime)
    {
        _runtime = runtime;
        _positionMs = 0;
        _positionTimer = new Timer(MainPositionTimerCallback, null, Timeout.Infinite, Timeout.Infinite);

        var remoteStateObservable = Spotify<R>.ObserveRemoteState()
            .Run(runtime)
            .ThrowIfFail()
            .ValueUnsafe()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Select(async c =>
            {
                try
                {
                    _positionMs = GetNewPosition((long)c.Position.TotalMilliseconds);
                    if (c.IsPaused)
                    {
                        _positionTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    }
                    else
                    {
                        _positionTimer.Change(0, TIMER_INTERVAL_MS);
                    }

                    _ = await c.TrackUri.MatchAsync(
                        async x =>
                        {
                            var track = (await Spotify<R>.GetTrack(x)
                                    .Run(runtime))
                                .ThrowIfFail();

                            CurrentTrack = track;
                            return unit;
                        },
                        () =>
                        {
                            CurrentTrack = null;
                            _positionTimer.Change(Timeout.Infinite, Timeout.Infinite);
                            return unit;
                        });
                }
                catch (Exception x)
                {
                    Debug.WriteLine(x);
                }
                return unit;
            })
            .Subscribe();
    }

    public long PositionMs => _positionMs;

    public ITrack? CurrentTrack
    {
        get => _currentTrack;
        set => this.RaiseAndSetIfChanged(ref _currentTrack, value);
    }

    private void MainPositionTimerCallback(object? state)
    {
        _positionMs = GetNewPosition(_positionMs);
    }

    private readonly object _positionLock = new();
    private long GetNewPosition(long position)
    {
        lock (_positionLock)
        {
            var theoreticalNext = position + TIMER_INTERVAL_MS;
            foreach (var (key, callback) in _positionCallbacks)
            {
                var previousMeasured = callback.PreviouslyMeasuredTimestamp;
                if (previousMeasured.IsNone)
                {
                    callback.PositionCallback(theoreticalNext);
                    _positionCallbacks[key] = callback with
                    {
                        PreviouslyMeasuredTimestamp = theoreticalNext
                    };
                }
                else
                {
                    var prevMeasured = previousMeasured.ValueUnsafe();
                    if (Math.Abs(theoreticalNext - prevMeasured) >= callback.MinimumDifference)
                    {
                        callback.PositionCallback(theoreticalNext);
                        _positionCallbacks[key] = callback with
                        {
                            PreviouslyMeasuredTimestamp = theoreticalNext
                        };
                    }
                }
            }
            return theoreticalNext;
        }
    }

    public Guid RegisterPositionCallback(int minDiff, Action<long> callback)
    {
        var id = Guid.NewGuid();
        var record = new PositionCallbackRecord(minDiff, callback, Option<long>.None);
        _positionCallbacks[id] = record;
        return id;
    }

    public void ClearPositionCallback(Guid id)
    {
        _positionCallbacks.Remove(id);
    }

    private readonly record struct PositionCallbackRecord(
        int MinimumDifference,
        Action<long> PositionCallback,
        Option<long> PreviouslyMeasuredTimestamp);
}