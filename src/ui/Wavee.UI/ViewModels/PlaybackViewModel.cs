using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Windows.Input;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using ReactiveUI;
using Wavee.Core.Contracts;
using Wavee.Spotify.Infrastructure.Remote.Messaging;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.Infrastructure.Traits;
using static LanguageExt.Prelude;
namespace Wavee.UI.ViewModels;

public sealed class PlaybackViewModel<R> : ReactiveObject where R : struct, HasSpotify<R>
{
    private readonly string _ownDeviceId;
    private const uint TIMER_INTERVAL_MS = 50; // 50 MS
    private readonly Dictionary<Guid, PositionCallbackRecord> _positionCallbacks = new();

    private SpotifyRemoteDeviceInfo _activeOnDevice = default;
    private long _positionMs;
    private readonly Timer _positionTimer;
    private readonly R _runtime;
    private ITrack? _currentTrack;
    private bool _paused;

    private double _volumePerc;
    public PlaybackViewModel(R runtime)
    {
        _runtime = runtime;
        _positionMs = 0;
        _positionTimer = new Timer(MainPositionTimerCallback, null, Timeout.Infinite, Timeout.Infinite);
        ResumePauseCommand = ReactiveCommand.CreateFromTask(PauseResume);
        _ownDeviceId = Spotify<R>.GetOwnDeviceId().Run(runtime).ThrowIfFail().ValueUnsafe();
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
                        Paused = true;
                    }
                    else
                    {
                        _positionTimer.Change(0, TIMER_INTERVAL_MS);
                        Paused = false;
                    }

                    var devices = c.Devices;
                    foreach (var potentialNewDevice in devices)
                    {
                        var oldDevice = Devices.FirstOrDefault(x => x.DeviceId == potentialNewDevice.Value.DeviceId);
                        if (oldDevice == default)
                        {
                            Devices.Add(potentialNewDevice.Value);
                        }
                        else
                        {
                            Devices.Remove(oldDevice);
                            Devices.Add(potentialNewDevice.Value);
                        }

                        if (c.ActiveDeviceId == potentialNewDevice.Value.DeviceId)
                        {
                            ActiveDevice = potentialNewDevice.Value;
                        }

                        if (c.ActiveDeviceId == _ownDeviceId)
                        {
                            ActiveDevice = default;
                        }
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

    public SpotifyRemoteDeviceInfo ActiveDevice
    {
        get => _activeOnDevice;
        set
        {
            this.RaiseAndSetIfChanged(ref _activeOnDevice, value);
            this.RaisePropertyChanged(nameof(CanControlVolume));
            this.RaisePropertyChanged(nameof(VolumePerc));
        }
    }
    public ObservableCollection<SpotifyRemoteDeviceInfo> Devices { get; } = new();

    public double VolumePerc => (ActiveDevice == default
                                 || string.Equals(ActiveDevice.DeviceId, _ownDeviceId))
        ? _volumePerc
        : ActiveDevice.Volume.Map(x => x * 100).IfNone(100);

    public bool CanControlVolume => (ActiveDevice == default
                                     || string.Equals(ActiveDevice.DeviceId, _ownDeviceId))
                                    || ActiveDevice.Volume.IsSome;

    public ITrack? CurrentTrack
    {
        get => _currentTrack;
        set => this.RaiseAndSetIfChanged(ref _currentTrack, value);
    }

    public bool Paused
    {
        get => _paused;
        set => this.RaiseAndSetIfChanged(ref _paused, value);
    }

    public bool ActiveOnThisDevice => ActiveDevice == default
                                      || string.Equals(ActiveDevice.DeviceId, _ownDeviceId);
    public ICommand ResumePauseCommand { get; }

    private async Task PauseResume(CancellationToken ct)
    {
        if (ActiveOnThisDevice)
        {
            //pause player
            return;
        }

        //remote command
        var paused = Paused;
        var aff =
            from remoteClient in Spotify<R>.GetRemoteClient()
            from _ in paused
                ? remoteClient.ValueUnsafe().Resume(ct).ToAff()
                : remoteClient.ValueUnsafe().Pause(ct).ToAff()
            select unit;

        var result = await aff.Run(_runtime);
    }


    private void MainPositionTimerCallback(object? state)
    {
        _positionMs = GetNewPosition(_positionMs);
    }

    private readonly object _positionLock = new();
    private bool _canControlVolume;

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