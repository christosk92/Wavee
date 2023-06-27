using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using ReactiveUI;
using Wavee.UI.Client.Playback;
using Wavee.UI.User;
using ReactiveUI;
using System.Reactive;
using LanguageExt.UnsafeValueAccess;

namespace Wavee.UI.ViewModel.Playback;


public sealed class PlaybackViewModel : ObservableObject
{
    private readonly object _lock = new();
    private record PositionCallback(Guid Id, int Difference, Action<int> PositionChanged, int PreviousMeasuredTime);
    private readonly List<PositionCallback> _positionCallbacks = new();
    private readonly Timer _timer;
    private const int TimerInterval = 10;

    private readonly IDisposable _subscription;
    private bool _hasPlayback;
    private ItemWithId _title;
    private string? _largeImageUrl;
    private string? _smallImageUrl;
    private ItemWithId[] _subtitles;
    private TimeSpan? _duration;
    private int _position;

    public PlaybackViewModel(UserViewModel user)
    {
        _subscription = user.Client.Playback
            .PlaybackEvents
            .ObserveOn(RxApp.MainThreadScheduler)
            .Select(OnPlaybackEvent)
            .Subscribe();

        _timer = new Timer(MainPositionCallback, null, Timeout.Infinite, Timeout.Infinite);
    }


    public bool HasPlayback
    {
        get => _hasPlayback;
        set => SetProperty(ref _hasPlayback, value);
    }

    public ItemWithId Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public ItemWithId[] Subtitles
    {
        get => _subtitles;
        set => SetProperty(ref _subtitles, value);
    }

    public string? LargeImageUrl
    {
        get => _largeImageUrl;
        set => SetProperty(ref _largeImageUrl, value);
    }

    public string? SmallImageUrl
    {
        get => _smallImageUrl;
        set => SetProperty(ref _smallImageUrl, value);
    }

    public TimeSpan? Duration
    {
        get => _duration;
        set => SetProperty(ref _duration, value);
    }

    private Unit OnPlaybackEvent(WaveeUIPlaybackState state)
    {
        HasPlayback = state.PlaybackState > 0;
        if (state.Metadata.IsSome)
        {
            var metadata = state.Metadata.ValueUnsafe();
            Title = metadata.Title;
            Subtitles = metadata.Subtitles;
            LargeImageUrl = metadata.LargeImageUrl;
            SmallImageUrl = metadata.SmallImageUrl;
            Duration = metadata.Duration;
        }
        switch (state.PlaybackState)
        {
            case WaveeUIPlayerState.NotPlayingAnything:
                HasPlayback = false;
                break;
            case WaveeUIPlayerState.Playing:
                HasPlayback = true;
                _timer.Change(0, TimerInterval);
                break;
            case WaveeUIPlayerState.Paused:
                HasPlayback = true;
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        SetPosition((int)state.Position.TotalMilliseconds);
        return Unit.Default;
    }

    private void SetPosition(int position)
    {
        lock (_lock)
        {
            _position = position;
            //notify all callbacks
            for (var index = 0; index < _positionCallbacks.Count; index++)
            {
                var callback = _positionCallbacks[index];
                callback.PositionChanged(position);
                //store the new position
                _positionCallbacks[index] = callback with { PreviousMeasuredTime = position };
            }
        }
    }
    private void MainPositionCallback(object? state)
    {
        lock (_lock)
        {
            var currentPos = _position;
            var theoreticalNext = currentPos + TimerInterval;

            for (var index = 0; index < _positionCallbacks.Count; index++)
            {
                var callback = _positionCallbacks[index];
                //check if we reached the difference 
                var diff = Math.Abs(currentPos - callback.PreviousMeasuredTime);
                if (diff >= callback.Difference)
                {
                    callback.PositionChanged(theoreticalNext);
                    //store the new position
                    _positionCallbacks[index] = callback with { PreviousMeasuredTime = theoreticalNext };
                }
                else
                {
                    //do nothing
                }
            }
            _position = theoreticalNext;
        }
    }

    public void ClearPositionCallback(Guid positionCallbackGuid)
    {
        lock (_lock)
        {
            var positionCallback = _positionCallbacks.FirstOrDefault(x => x.Id == positionCallbackGuid);
            if (positionCallback is not null)
            {
                _positionCallbacks.Remove(positionCallback);
            }
        }
    }

    public Guid RegisterPositionCallback(int difference, Action<int> positionChanged)
    {
        lock (_lock)
        {
            var positionCallback = new PositionCallback(Guid.NewGuid(), difference, positionChanged, 0);
            _positionCallbacks.Add(positionCallback);
            return positionCallback.Id;
        }
    }
}
