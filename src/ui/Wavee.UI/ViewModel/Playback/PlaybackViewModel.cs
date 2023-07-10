using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CommunityToolkit.Mvvm.ComponentModel;
using ReactiveUI;
using Wavee.UI.Client.Playback;
using Wavee.UI.User;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Remote;
using Unit = System.Reactive.Unit;

namespace Wavee.UI.ViewModel.Playback;


public sealed class PlaybackViewModel : ObservableObject
{
    private readonly object _lock = new();

    private record PositionCallback(Guid Id, int Difference, Action<int> PositionChanged, int PreviousMeasuredTime);

    private Stopwatch _timeAfterPositionSetSw = new Stopwatch();
    private TimeSpan _positionSince = TimeSpan.Zero;

    private readonly List<PositionCallback> _positionCallbacks = new();
    private readonly Timer _timer;
    private const int TimerInterval = 10;


    private readonly IDisposable _subscription;
    private bool _hasPlayback;
    private ItemWithId _title;
    private string? _largeImageUrl;
    private string? _smallImageUrl;
    private string? _mediumImageUrl;
    private ItemWithId[] _subtitles;
    private TimeSpan? _duration;
    private string _itemId;
    private Option<string> _uid;
    private bool _paused;
    private readonly UserViewModel _user;
    private readonly Subject<WaveeUIPlaybackState> _playbackEvent = new Subject<WaveeUIPlaybackState>();
    public WaveeUIPlaybackState _lastReceivedState;
    private bool _hasLyrics;
    private RemoteDeviceInfo _remoteDevice;
    private string _ourDeviceId;

    public PlaybackViewModel(UserViewModel user)
    {
        _user = user;
        _subscription = user.Client.Playback
            .PlaybackEvents
            .ObserveOn(RxApp.MainThreadScheduler)
            .Select(OnPlaybackEvent)
            .Subscribe();
        _ourDeviceId = user.Client.Playback.OurDeviceId;
        _timer = new Timer(MainPositionCallback, null, Timeout.Infinite, Timeout.Infinite);
    }


    public bool HasPlayback
    {
        get => _hasPlayback;
        set => SetProperty(ref _hasPlayback, value);
    }

    public string ItemId
    {
        get => _itemId;
        set => SetProperty(ref _itemId, value);
    }

    public Option<string> Uid
    {
        get => _uid;
        set => SetProperty(ref _uid, value);
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
    public string? MediumImageUrl
    {
        get => _mediumImageUrl;
        set => SetProperty(ref _mediumImageUrl, value);
    }

    public TimeSpan? Duration
    {
        get => _duration;
        set => SetProperty(ref _duration, value);
    }

    public bool Paused
    {
        get => _paused;
        set => SetProperty(ref _paused, value);
    }

    public bool HasLyrics
    {
        get => _hasLyrics;
        set => SetProperty(ref _hasLyrics, value);
    }
    public ObservableCollection<RemoteDeviceInfo> Devices { get; } = new();

    public RemoteDeviceInfo RemoteDevice
    {
        get => _remoteDevice;
        set
        {
            if (SetProperty(ref _remoteDevice, value))
            {
                this.OnPropertyChanged(nameof(IsPlayingOnRemoteDevice));
            }
        }
    }
    public bool IsPlayingOnRemoteDevice => RemoteDevice != default && RemoteDevice.DeviceId != _ourDeviceId;


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
            MediumImageUrl = metadata.MediumImageUrl;
            Duration = metadata.Duration;
            ItemId = metadata.Id;
            Uid = metadata.Uid;
            HasLyrics = metadata.HasLyrics;
        }
        else
        {
            HasLyrics = false;
        }

        Devices.Clear();
        foreach (var otherDevices in state.Devices)
        {
            Devices.Add(otherDevices);
        }

        if (state.Remote.IsSome)
        {
            var remoteState = state.Remote.ValueUnsafe();
            if (remoteState.DeviceId == _ourDeviceId)
            {
                RemoteDevice = default;
            }
            else
            {
                RemoteDevice = remoteState;
            }
        }
        else
        {
            RemoteDevice = default;
        }

        switch (state.PlaybackState)
        {
            case WaveeUIPlayerState.NotPlayingAnything:
                HasLyrics = false;
                HasPlayback = false;
                break;
            case WaveeUIPlayerState.Playing:
                HasPlayback = true;
                _timer.Change(0, TimerInterval);
                Paused = false;
                break;
            case WaveeUIPlayerState.Paused:
                HasPlayback = true;
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                Paused = true;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        SetPosition((int)state.Position.TotalMilliseconds);
        _playbackEvent.OnNext(state);
        _lastReceivedState = state;
        return Unit.Default;
    }

    private void SetPosition(int position)
    {
        lock (_lock)
        {
            _positionSince = TimeSpan.FromMilliseconds(position);
            _timeAfterPositionSetSw.Restart();
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
            var elapsed = _timeAfterPositionSetSw.ElapsedMilliseconds;
            var currentPos = (int)(_positionSince.TotalMilliseconds + elapsed);

            for (var index = 0; index < _positionCallbacks.Count; index++)
            {
                var callback = _positionCallbacks[index];
                //check if we reached the difference 
                var diff = Math.Abs(currentPos - callback.PreviousMeasuredTime);
                if (diff >= callback.Difference)
                {
                    callback.PositionChanged(currentPos);
                    //store the new position
                    _positionCallbacks[index] = callback with { PreviousMeasuredTime = currentPos };
                }
                else
                {
                    //do nothing
                }
            }

            //if we reached the end of the song, stop the timer
            if (Duration is not null && currentPos >= Duration.Value.TotalMilliseconds)
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
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

    public IObservable<WaveeUIPlaybackState> CreateListener()
    {
        return _playbackEvent
            .StartWith(_user.Client.Playback.CurrentPlayback)
            .ObserveOn(RxApp.MainThreadScheduler);
    }

    // public double GetPosition()
    // {
    //     return _position;
    // }
}
