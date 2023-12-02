using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Wavee.UI.Test;

namespace Wavee.UI.Features.Playback.ViewModels;

public abstract class PlaybackPlayerViewModel : ObservableObject
{
    private record PositionCallback(int MinimumDifferenceMs, Action<TimeSpan> Callback, int previousMs);

    private TimeSpan? _timeSinceStopwatch;
    private Stopwatch? _stopwatch;
    private Dictionary<Guid, PositionCallback> _positionCallbacks = new();

    private readonly Timer _timer;
    private bool _isActive;
    private bool _hasPlayback;
    private string? _coverSmallImageUrl;
    private string? _title;
    private string[]? _artists;
    private TimeSpan _duration;

    private readonly IUIDispatcher _dispatcher;
    protected PlaybackPlayerViewModel(IUIDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _timer = new Timer(state =>
        {
            var time = _timeSinceStopwatch + _stopwatch?.Elapsed;
            if (time is not null)
            {
                foreach (var callback in _positionCallbacks)
                {
                    var (minimumDifferenceMs, action, previousMs) = callback.Value;
                    var currentMs = (int)time.Value.TotalMilliseconds;
                    var diff = currentMs - previousMs;
                    if (diff is < 0 || diff >= minimumDifferenceMs)
                    {
                        _dispatcher.Invoke(() => action(time.Value));
                        _positionCallbacks[callback.Key] = callback.Value with { previousMs = currentMs };
                    }
                }
            }
        }, null, -1, -1);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public bool HasPlayback
    {
        get => _hasPlayback;
        protected set => SetProperty(ref _hasPlayback, value);
    }

    public string? CoverSmallImageUrl
    {
        get => _coverSmallImageUrl;
        protected set => SetProperty(ref _coverSmallImageUrl, value);
    }

    public string? Title
    {
        get => _title;
        protected set => SetProperty(ref _title, value);
    }

    public string[]? Artists
    {
        get => _artists;
        protected set => SetProperty(ref _artists, value);
    }

    public TimeSpan Duration
    {
        get => _duration;
        protected set => SetProperty(ref _duration, value);
    }

    public Guid AddPositionCallback(int minimumDifferenceMs, Action<TimeSpan> callback)
    {
        var id = Guid.NewGuid();
        _positionCallbacks.Add(id, new PositionCallback(minimumDifferenceMs, callback, 0));
        return id;
    }


    public void Activate()
    {
        IsActive = true;
    }

    protected void Pause()
    {
        if (!HasPlayback) return;
        _stopwatch?.Stop();
        _timer.Change(-1, -1);
    }

    protected void Resume(TimeSpan? at = null)
    {
        if (!HasPlayback) return;

        //Tick every 10ms
        _timeSinceStopwatch = at ?? _timeSinceStopwatch ?? TimeSpan.Zero;
        _stopwatch = Stopwatch.StartNew();
        _timer.Change(0, 10);
    }
}