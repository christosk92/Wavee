using System.Collections.Immutable;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Windows.Input;
using Eum.Spotify.connectstate;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using ReactiveUI;
using Spotify.Metadata;
using Wavee.Player;
using Wavee.Spotify.Infrastructure.Mercury.Models;
using Wavee.Spotify.Infrastructure.PrivateApi.Contracts.Response;
using Wavee.Spotify.Infrastructure.Remote.Contracts;

namespace Wavee.UI.ViewModels.Playback;

public sealed partial class PlaybackViewModel : ReactiveObject
{
    private readonly object _positionLock = new();
    private const uint TIMER_INTERVAL_MS = 50; // 50 MS
    private readonly Dictionary<Guid, PositionCallbackRecord> _positionCallbacks = new();

    private SpotifyRemoteDeviceInfo _device;
    private bool _isConnectedToRemoteState;
    private bool _isLoadingItem;
    private bool _paused;
    private RepeatState _repeatState;
    private bool _shuffling;
    private TrackOrEpisode? _currentTrack;
    private double _volume;
    private bool _hasLyrics;
    private readonly Timer _positionTimer;
    private long _positionMs;

    private readonly PlaybackViewModelUpdates _updates;
    private IReadOnlyCollection<SpotifyRemoteDeviceInfo> _devices;
    private SpotifyColors _currentTrackColors;

    public PlaybackViewModel()
    {
        _positionMs = 0;
        _positionTimer = new Timer(MainPositionTimerCallback, null, Timeout.Infinite, Timeout.Infinite);
        _updates = new PlaybackViewModelUpdates(this);

        bool IsPlayingOnThisDevice() => Device.DeviceId == State.Instance.Client.DeviceId;

        ResumePauseCommand = ReactiveCommand.CreateFromTask((ct) =>
        {
            if (IsPlayingOnThisDevice())
            {
                if (_paused)
                {
                    WaveePlayer.Instance.Resume();
                }
                else
                {
                    WaveePlayer.Instance.Pause();
                }
                return Task.CompletedTask;
            }

            if (_paused)
            {
                return State.Instance.Client.Remote.Resume(ct);
            }
            else
            {
                return State.Instance.Client.Remote.Pause(ct);
            }
        });

        ToggleShuffleCommand = ReactiveCommand.CreateFromTask((ct) =>
        {
            if (IsPlayingOnThisDevice())
            {
                WaveePlayer.Instance.SetShuffle(!_shuffling);
                return Task.CompletedTask;
            }

            return State.Instance.Client.Remote.SetShuffle(!_shuffling, ct);
        });

        SkipNextCommand = ReactiveCommand.CreateFromTask((ct) =>
        {
            if (IsPlayingOnThisDevice())
            {
                return WaveePlayer.Instance.SkipNext(false, true)
                    .Map(_ => default(Unit))
                    .AsTask();
            }

            return State.Instance.Client.Remote.SkipNext(ct);
        });

        ToggleRepeatCommand = ReactiveCommand.CreateFromTask((ct) =>
        {
            static RepeatState Next(RepeatState state)
            {
                return state switch
                {
                    RepeatState.None => RepeatState.Context,
                    RepeatState.Context => RepeatState.Track,
                    RepeatState.Track => RepeatState.None,
                    _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
                };
            }

            var next = Next(_repeatState);
            if (IsPlayingOnThisDevice())
            {
                WaveePlayer.Instance.SetRepeat(next);
                return Task.CompletedTask;
            }

            return State.Instance.Client.Remote.SetRepeat(next, ct);
        });
    }

    public double Volume
    {
        get => _volume;
        set
        {
            this.RaiseAndSetIfChanged(ref _volume, value);
            this.RaisePropertyChanged(nameof(VolumePerc));
        }
    }
    public SpotifyColors CurrentTrackColors
    {
        get => _currentTrackColors;
        set => this.RaiseAndSetIfChanged(ref _currentTrackColors, value);
    }
    public TrackOrEpisode? CurrentTrack
    {
        get => _currentTrack;
        set => this.RaiseAndSetIfChanged(ref _currentTrack, value);
    }
    public bool Shuffling
    {
        get => _shuffling;
        set => this.RaiseAndSetIfChanged(ref _shuffling, value);
    }

    public SpotifyRemoteDeviceInfo Device => Devices?.SingleOrDefault(x => x.IsActive,
        new SpotifyRemoteDeviceInfo(State.Instance.Client.DeviceId, string.Empty, DeviceType.Unknown, true, 1))
    ?? new SpotifyRemoteDeviceInfo(State.Instance.Client.DeviceId, string.Empty, DeviceType.Unknown, true, 1);

    public IReadOnlyCollection<SpotifyRemoteDeviceInfo> Devices
    {
        get => _devices;
        set
        {
            this.RaiseAndSetIfChanged(ref _devices, value);
            this.RaisePropertyChanged(nameof(Device));
            if (Device.DeviceId != State.Instance.Client.DeviceId)
            {
                Volume = Device.Volume.IfNone(1);
            }
        }
    }

    public bool HasLyrics
    {
        get => _hasLyrics;
        set => this.RaiseAndSetIfChanged(ref _hasLyrics, value);
    }

    public RepeatState RepeatState
    {
        get => _repeatState;
        set => this.RaiseAndSetIfChanged(ref _repeatState, value);
    }
    public bool Paused
    {
        get => _paused;
        set => this.RaiseAndSetIfChanged(ref _paused, value);
    }
    public int VolumePerc => (int)(Volume * 100);
    public bool IsConnectedToRemoteState
    {
        get => _isConnectedToRemoteState;
        set
        {
            this.RaiseAndSetIfChanged(ref _isConnectedToRemoteState, value);
            this.RaisePropertyChanged(nameof(IsLoading));
        }
    }

    public bool IsLoadingItem
    {
        get => _isLoadingItem;
        set
        {
            this.RaiseAndSetIfChanged(ref _isLoadingItem, value);
            this.RaisePropertyChanged(nameof(IsLoading));
        }
    }

    public bool IsLoading => !IsConnectedToRemoteState || IsLoadingItem;
    public ICommand ResumePauseCommand { get; }
    public ICommand SkipNextCommand { get; }
    public ICommand ToggleRepeatCommand { get; }
    public ICommand SkipPreviousCommand { get; }
    public ICommand ToggleShuffleCommand { get; }
    public ICommand MuteOrRestoreVolumeCommand { get; }


    public Task<Unit> SeekToAsync(double to, CancellationToken ct = default)
    {
        if (CurrentTrack is null)
        {
            return Task.FromResult(default(Unit));
        }

        if (Device.DeviceId == State.Instance.Client.DeviceId)
        {
            WaveePlayer.Instance.SeekTo(TimeSpan.FromMilliseconds(to));
            return Task.FromResult(default(Unit));
        }

        return State.Instance.Client.Remote.SeekTo(TimeSpan.FromMilliseconds(to), ct);
    }
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
    private void MainPositionTimerCallback(object? state)
    {
        var to = GetNewPosition(_positionMs);
        lock (_positionLock)
        {
            _positionMs = to;
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

    private class PlaybackViewModelUpdates
    {

        private readonly PlaybackViewModel _inner;
        public PlaybackViewModelUpdates(PlaybackViewModel inner)
        {
            _inner = inner;
            SpotifyView.ObserveRemoteState()
                .SelectMany(async x =>
                {
                    var currentTrack = _inner._currentTrack;
                    if (x.TrackUri.IsSome && x.TrackUri.ValueUnsafe() != _inner.CurrentTrack?.Id)
                    {
                        var track = await SpotifyView.GetTrack(x.TrackUri.ValueUnsafe()).Run();
                        if (track.IsFail)
                        {
                            var err = track.Match(Succ: _ => throw new NotSupportedException(), Fail: y => y);
                            Debug.WriteLine(err);
                            return (x, null);
                        }
                        return (x, track.Match(Succ: y => y, Fail: _ => throw new NotSupportedException()));
                    }
                    else if (x.TrackUri.IsSome)
                    {
                        return (x, currentTrack);
                    }
                    return (x, null);
                })
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(UpdateViewModel);

            WaveePlayer.Instance
                .StateUpdates
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(UpdateViewModelFromPlayer);

            this.WhenAnyValue(x => x._inner.CurrentTrack)
                .Where(x => x is not null)
                .SelectMany(async x =>
                {
                    var color = await SpotifyView.GetColorForImage(x.GetImage(Image.Types.Size.Default))
                        .Run();
                    if (color.IsFail)
                    {
                        var err = color.Match(Succ: _ => throw new NotSupportedException(), Fail: y => y);
                        Debug.WriteLine(err);
                        return new SpotifyColors();
                    }

                    return color.Match(Succ: y => y, Fail: _ => throw new NotSupportedException());
                })
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(color =>
                {
                    _inner.CurrentTrackColors = color;
                });
        }

        private void UpdateViewModelFromPlayer(WaveePlayerState state)
        {
            if (state.TrackDetails.IsNone)
            {
                //reset state
                SetLoadingItem();
                SetPaused(); ;
                SetTrack(null);
                _inner.CurrentTrackColors = new SpotifyColors();
                SetPosition(0);
                return;
            }

            var devicesItems = new List<SpotifyRemoteDeviceInfo>
            {
                new SpotifyRemoteDeviceInfo(
                   DeviceId: State.Instance.Client.DeviceId,
                   DeviceName: State.Instance.Config.Remote.DeviceName,
                   DeviceType: State.Instance.Config.Remote.DeviceType,
                   IsActive: true,
                   Volume: 0.5)
            };
            foreach (var device in _inner.Devices)
            {
                if (device.DeviceId != State.Instance.Client.DeviceId)
                {
                    devicesItems.Add(device);
                }
            }
            _inner.Devices = devicesItems.ToImmutableList();
            var track = state.TrackDetails.ValueUnsafe();
            if (track.Id != _inner.CurrentTrack?.Id)
            {
                var trackOrEpisode = track.Metadata["track_or_episode"] as TrackOrEpisode;
                SetTrack(trackOrEpisode);
            }

            if (state.IsPaused)
            {
                SetPaused();
            }
            else
            {
                SetPlaying();
            }

            SetOptions(state.IsShuffling, state.RepeatState);
            SetPosition((long)WaveePlayer.Instance.Position.IfNone(TimeSpan.Zero).TotalMilliseconds);
        }

        private void UpdateViewModel(
            (SpotifyRemoteState state, TrackOrEpisode? trackData) data)
        {
            _inner.IsConnectedToRemoteState = true;
            SetDevices(data.state.Devices
                .Select(c => c.Value)
                .ToImmutableList());

            if (_inner.Device.DeviceId != State.Instance.Client.DeviceId)
            {
                SetTrack(data.trackData);

                if (data.state.IsBuffering || data.state.TrackUri.IsNone || data.trackData is null)
                {
                    SetLoadingItem();
                }
                else if (data.state.IsPaused)
                {
                    SetPaused();
                }
                else if (data.state.IsPlaying)
                {
                    SetPlaying();
                }

                SetPosition((long)data.state.Position.TotalMilliseconds);

                SetOptions(data.state.IsShuffling, data.state.RepeatState);
            }
        }

        private void SetLoadingItem()
        {
            _inner.IsLoadingItem = true;
        }

        private void SetDevices(IReadOnlyCollection<SpotifyRemoteDeviceInfo> devices)
        {
            _inner.Devices = devices;
        }
        private void SetTrack(TrackOrEpisode? track)
        {
            _inner.CurrentTrack = track;
            _inner.IsLoadingItem = track is null;
        }
        private void SetPaused()
        {
            _inner.Paused = true;
            _inner._positionTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void SetPlaying()
        {
            _inner.Paused = false;
            _inner._positionTimer.Change(0, TIMER_INTERVAL_MS);
        }

        private void SetPosition(long position)
        {
            var to = _inner.GetNewPosition(position);
            lock (_inner._positionLock)
            {
                _inner._positionMs = to;
            }
        }

        private void SetOptions(bool isShuffling, RepeatState repeatState)
        {
            _inner.Shuffling = isShuffling;
            _inner.RepeatState = repeatState;
        }
    }

}