using CommunityToolkit.Mvvm.ComponentModel;
using LanguageExt.Common;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Eum.Spotify.connectstate;
using LanguageExt.UnsafeValueAccess;
using Wavee.UI.Providers;
using Wavee.UI.Services;
using Wavee.UI.ViewModels.Library;

namespace Wavee.UI.ViewModels.NowPlaying;

public sealed class NowPlayingViewModel : ObservableObject, IHasProfileViewModel
{
    private readonly List<IWaveeUIAuthenticatedProfile> _profiles = [];
    private readonly IDispatcher _dispatcherWrapper;

    private (IWaveePlayableItem Item, IWaveeUIAuthenticatedProfile Profile) _currentTrack = (null, null);
    private bool _isBusy;
    private bool _isPaused;
    private bool _isShuffling;
    private WaveeRepeatStateType _repeatState;
    private TimeSpan _positionOffset = TimeSpan.Zero;
    private Stopwatch _positionSw = new Stopwatch();
    private SemaphoreSlim _positionLock = new SemaphoreSlim(1, 1);

    private readonly Dictionary<Guid, Action> _timerCallbacks;
    private readonly ManualResetEvent _pauseEvent = new(false);
    private WaveeRemoteDeviceViewModel? _activeDevice;
    private double? _volume;

    public NowPlayingViewModel(IDispatcher dispatcherWrapper)
    {
        _dispatcherWrapper = dispatcherWrapper;
        _timerCallbacks = new Dictionary<Guid, Action>();
        Errors = new ObservableCollection<ExceptionForProfile>();
        RemoteDevices = new ObservableCollection<WaveeRemoteDeviceViewModel>();
        Restrictions = new ObservableCollection<WaveePlaybackRestrictionType>();
        ThreadPool.QueueUserWorkItem(async x =>
        {
            while (true)
            {
                _pauseEvent.WaitOne();
                _positionLock.Wait();
                foreach (var (_, callback) in _timerCallbacks)
                {
                    callback();
                }
                _positionLock.Release();

                await Task.Delay(10).ConfigureAwait(false);
            }
        });

        PlayPauseCommand = new AsyncRelayCommand(async () =>
        {
            if (ActiveDevice is null)
            {
                // pause/play on device
            }
            else
            {
                var profile = _profiles.Last();
                if (IsPaused) await profile.ResumeRemoteDevice(true);
                else await profile.PauseRemoteDevice(true);
            }
        });

        SkipPrevCommand = new AsyncRelayCommand(async () =>
        {
            if (ActiveDevice is null)
            {
                // pause/play on device
            }
            else
            {
                var profile = _profiles.Last();
                await profile.SkipPrevious(true);
            }
        }, CanSkipPrev);
        SkipNextCommand = new AsyncRelayCommand(async () =>
        {
            if (ActiveDevice is null)
            {
                // pause/play on device
            }
            else
            {
                var profile = _profiles.Last();
                await profile.SkipNext(true);
            }
        }, CanSkipNext);

        ToggleShuffleCommand = new AsyncRelayCommand(async () =>
        {
            if (ActiveDevice is null)
            {
                // pause/play on device
            }
            else
            {
                var profile = _profiles.Last();
                var nextShuffleState = !IsShuffling;
                IsShuffling = nextShuffleState;
                await profile.SetShuffle(IsShuffling, true);
            }
        }, CanShuffle);

        int totalRepeatStates = Enum.GetValues<WaveeRepeatStateType>().Length;
        NextRepeatStateCommand = new AsyncRelayCommand(async () =>
        {
            var currRepatState = (int)this.RepeatState;
            var nextRepeatState = (currRepatState + 1) % totalRepeatStates;
            var nextRepeatStateType = (WaveeRepeatStateType)nextRepeatState;
            RepeatState = nextRepeatStateType;

            if (ActiveDevice is null)
            {
                // pause/play on device
            }
            else
            {
                var profile = _profiles.Last();
                await profile.GoToRepeatState(nextRepeatStateType, true);
            }
        }, CanToggleRepeatState);

        SeekCommand = new AsyncRelayCommand<TimeSpan>(async x =>
        {
            await _positionLock.WaitAsync();
            _positionOffset = x;
            _positionSw = Stopwatch.StartNew();

            if (ActiveDevice is null)
            {
                // pause/play on device
            }
            else
            {
                var profile = _profiles.Last();
                await profile.SeekTo(x, true);
            }

            _positionLock.Release();
        });

        SetVolumeCommand = new AsyncRelayCommand<double>(async x =>
        {
            if (ActiveDevice is null)
            {
                // pause/play on device
            }
            else
            {
                Volume = x;
                x /= 100;

                x = Math.Max(Math.Min(x, 1), 0);
                ActiveDevice.Volume = (float)x;
                var profile = _profiles.Last();
                await profile.SetVolume(x, true);
            }
        }, CanSetVolume);
    }

    private bool CanSetVolume(double obj)
    {
        return ActiveDevice is null || ActiveDevice.Volume is not null;
    }

    private bool CanSkipPrev()
    {
        return Restrictions.All(x => x is not WaveePlaybackRestrictionType.SkipPrevious);
    }
    private bool CanSkipNext()
    {
        return Restrictions.All(x => x is not WaveePlaybackRestrictionType.SkipNext);
    }
    private bool CanShuffle()
    {
        return Restrictions.All(x => x is not WaveePlaybackRestrictionType.CannotShuffle);
    }
    private bool CanToggleRepeatState()
    {
        bool canRepeatContext = true;
        bool canRepeatTrack = true;
        foreach (var restriction in Restrictions)
        {
            if (restriction is WaveePlaybackRestrictionType.CannotRepeatContext) canRepeatContext = false;
            if (restriction is WaveePlaybackRestrictionType.CannotRepeatTrack) canRepeatTrack = false;
        }

        return canRepeatTrack && canRepeatContext;
    }

    public (IWaveePlayableItem Item, IWaveeUIAuthenticatedProfile Profile) CurrentTrack
    {
        get => _currentTrack;
        private set
        {
            if (value.Item?.Id != _currentTrack.Item?.Id)
            {
                this.SetProperty(ref _currentTrack, value);
            }
        }
    }

    public bool IsPaused
    {
        get => _isPaused;
        private set => this.SetProperty(ref _isPaused, value);
    }
    public ObservableCollection<WaveePlaybackRestrictionType> Restrictions { get; }
    public bool IsBusy
    {
        get => _isBusy;
        private set => this.SetProperty(ref _isBusy, value);
    }
    public bool IsShuffling
    {
        get => _isShuffling;
        private set => this.SetProperty(ref _isShuffling, value);
    }
    public WaveeRemoteDeviceViewModel? ActiveDevice
    {
        get => _activeDevice;
        private set => this.SetProperty(ref _activeDevice, value);
    }
    public double? Volume
    {
        get => _volume;
        set
        {
            if (this.SetProperty(ref _volume, value))
            {
                VolumeChanged?.Invoke(this, value);
            }
        }
    }
    public WaveeRepeatStateType RepeatState
    {
        get => _repeatState;
        private set => this.SetProperty(ref _repeatState, value);
    }
    public bool HasErrors => Errors.Count > 0;
    public ObservableCollection<ExceptionForProfile> Errors { get; }
    public ObservableCollection<WaveeRemoteDeviceViewModel> RemoteDevices { get; }
    public event EventHandler<double?>? VolumeChanged;
    public TimeSpan Position => _positionSw.Elapsed + _positionOffset;
    public AsyncRelayCommand PlayPauseCommand { get; }
    public AsyncRelayCommand SkipPrevCommand { get; }
    public AsyncRelayCommand SkipNextCommand { get; }
    public AsyncRelayCommand NextRepeatStateCommand { get; }
    public AsyncRelayCommand ToggleShuffleCommand { get; }
    public AsyncRelayCommand<TimeSpan> SeekCommand { get; }
    public AsyncRelayCommand<double> SetVolumeCommand { get; }
    public bool SetVolumeCommandCanExecute => SetVolumeCommand.CanExecute(1);

    public void AddFromProfile(IWaveeUIAuthenticatedProfile profile)
    {
        _profiles.Add(profile);


        Task.Run(async () => await CreateListener(profile, false));
    }

    public void RemoveFromProfile(IWaveeUIAuthenticatedProfile profile)
    {
        _profiles.Remove(profile);
    }


    private async Task CreateListener(IWaveeUIAuthenticatedProfile profile, bool isRetrying)
    {
        _dispatcherWrapper.Dispatch(() =>
        {
            IsBusy = true;
            ClearErrorsFor(profile);
        }, highPriority: true);
        if (isRetrying)
        {
            await Task.Delay(TimeSpan.FromSeconds(.5));
        }
        try
        {
            profile.PlaybackStateChanged -= ProfileOnPlaybackStateChanged;

            var playbackState = await profile.ConnectToRemoteStateIfApplicable();
            ProfileOnPlaybackStateChanged(profile, playbackState);
            profile.PlaybackStateChanged += ProfileOnPlaybackStateChanged;

            _dispatcherWrapper.Dispatch(() =>
            {
                IsBusy = false;
            }, highPriority: false);
        }
        catch (Exception x)
        {
            _dispatcherWrapper.Dispatch(() =>
            {
                AddError(profile, x, () => Task.Run(async () => await CreateListener(profile, true)));
                IsBusy = false;
            }, highPriority: false);
        }
    }

    private void ProfileOnPlaybackStateChanged(object? sender, WaveeUIPlaybackState e)
    {
        _dispatcherWrapper.Dispatch(() =>
        {
            CurrentTrack = (e.Item, sender as IWaveeUIAuthenticatedProfile)!;
            IsShuffling = e.IsShuffling;
            RepeatState = e.RepeatState;
            IsPaused = e.IsPaused;

            _positionLock.Wait();
            _positionSw = e.PositionSw;
            _positionOffset = e.PositionOffset;
            _positionLock.Release();

            if (e.IsPaused)
                _pauseEvent.Reset();
            else
                _pauseEvent.Set();

            foreach (var newDevice in e.Devices)
            {
                var existingDevice = RemoteDevices.FirstOrDefault(x => x.Id == newDevice.Id);
                if (existingDevice is null)
                {
                    RemoteDevices.Add(new WaveeRemoteDeviceViewModel(newDevice));
                }
                else
                {
                    existingDevice.Name = newDevice.Name;
                    if (newDevice.Volume.IsSome)
                    {
                        existingDevice.Volume = newDevice.Volume.ValueUnsafe();
                    }
                    else
                    {
                        existingDevice.Volume = null;
                    }

                    existingDevice.Type = newDevice.Type;
                    existingDevice.IsActive = newDevice.IsActive;
                }
            }


            ActiveDevice = RemoteDevices.FirstOrDefault(x => x.IsActive);
            this.OnPropertyChanged(nameof(ActiveDevice));

            foreach (var existingDevice in RemoteDevices.ToList())
            {
                var isInNewDevice = e.Devices.Any(x => x.Id == existingDevice.Id);
                if (!isInNewDevice)
                {
                    RemoteDevices.Remove(existingDevice);
                }
            }

            Restrictions.Clear();
            foreach (var restrictions in e.Restrictions)
            {
                Restrictions.Add(restrictions);
            }

            ToggleShuffleCommand.NotifyCanExecuteChanged();
            NextRepeatStateCommand.NotifyCanExecuteChanged();
            SkipPrevCommand.NotifyCanExecuteChanged();
            SkipNextCommand.NotifyCanExecuteChanged();
            SetVolumeCommand.NotifyCanExecuteChanged();
            this.OnPropertyChanged(nameof(SetVolumeCommandCanExecute));
            if (ActiveDevice is not null)
            {
                if (ActiveDevice.Volume is null) Volume = null;
                else Volume = ActiveDevice.Volume * 100;
            }
        }, true);
    }

    private void AddError(IWaveeUIAuthenticatedProfile profile, Exception err, Action? retry)
    {
        Errors.Add(new ExceptionForProfile(err, profile, retry));

        this.OnPropertyChanged(nameof(HasErrors));
    }
    private void ClearErrorsFor(IWaveeUIAuthenticatedProfile profile)
    {
        var errors = Errors.Where(x => x.Profile == profile).ToArray();
        foreach (var error in errors)
        {
            Errors.Remove(error);
        }

        this.OnPropertyChanged(nameof(HasErrors));
    }

    public Guid RegisterTimerCallback(Action callback)
    {
        var id = Guid.NewGuid();
        _timerCallbacks.Add(id, callback);
        return id;
    }

    public void ClearPositionCallback(Guid callback)
    {
        _timerCallbacks.Remove(callback);
    }
}

public enum WaveePlaybackRestrictionType
{
    CannotShuffle,
    CannotRepeatContext,
    CannotRepeatTrack,
    SkipNext,
    SkipPrevious
}

public sealed class WaveeRemoteDeviceViewModel : ObservableObject
{
    private bool _isActive;
    private float? _volume;

    public WaveeRemoteDeviceViewModel(WaveeUIRemoteDevice newDevice)
    {
        Id = newDevice.Id;
        Name = newDevice.Name;
        Type = newDevice.Type;
        if (newDevice.Volume.IsSome)
        {
            Volume = newDevice.Volume.ValueUnsafe();
        }
        else
        {
            Volume = null;
        }

        IsActive = newDevice.IsActive;
    }

    public string Id { get; }
    public string Name { get; set; }
    public DeviceType Type { get; set; }

    public float? Volume
    {
        get => _volume;
        set => this.SetProperty(ref _volume, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => this.SetProperty(ref _isActive, value);
    }
}