using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using ReactiveUI;
using Wavee.UI.Identity.Users;
using Wavee.UI.Identity.Users.Contracts;
using Wavee.UI.Models;
using Wavee.UI.Navigation;
using Wavee.UI.Utils;
using Wavee.UI.ViewModels.Identity.User;
using Wavee.UI.ViewModels.Playback.Impl;
using Wavee.UI.ViewModels.Playback.PlayerEvents;

namespace Wavee.UI.ViewModels.Playback;

public partial class PlayerViewModel : ObservableRecipient, IDisposable
{
    private const ushort INCREMENT_BY_MS = 50;

    private readonly CancellationTokenSource _cts;
    private readonly PlayerViewHandlerInternal _handlerInternal;
    private readonly ILogger<PlayerViewModel>? _logger;

    private readonly ConcurrentDictionary<ulong, (ulong prev, Dictionary<Guid, Action<ulong>>)> _positionCallbacks =
        new();

    private readonly object _positionLock = new();
    private readonly Timer _positionTimespan;

    private readonly AsyncAutoResetEvent _waitForSeekResponse = new();

    [ObservableProperty] private bool _paused = true;

    [ObservableProperty] private PlayingTrackView? _playingItem;

    //  [ObservableProperty] private bool _coverImageExpanded;

    // [ObservableProperty]
    private ulong _positionMs;

    public WaveeUserViewModel CurrentUser
    {
        get;
    }

    public PlayerViewModel(ServiceType userServiceType, WaveeUserViewModel currentUser, ILogger<PlayerViewModel>? logger)
    {
        _logger = logger;
        CurrentUser = currentUser;
        _handlerInternal = userServiceType switch
        {
            ServiceType.Local => Ioc.Default.GetRequiredService<LocalPlayerHandler>(),
            ServiceType.Spotify => new SpotifyPlayerHandler(),
            _ => throw new ArgumentOutOfRangeException(nameof(userServiceType), userServiceType, null)
        };
        _positionTimespan = new Timer(IncrementPosition, null, Timeout.Infinite, Timeout.Infinite);
        _cts = new CancellationTokenSource();
        Task.Factory.StartNew(EventsReaderLoop, TaskCreationOptions.LongRunning);
        Instance = this;
        NavigateTo = new RelayCommand<IDescriptionaryItem>(item =>
        {
            NavigationService.Instance.To(item.NavigateTo, item.Value);
        });
    }


    //public ICommand NavigateTo => Commands.NavigateToCommand;

    public void Dispose() => _handlerInternal.Dispose();

    [RelayCommand]
    public Task PlayTask(IPlayContext? arg)
    {
        if (arg == null)
        {
            return Task.CompletedTask;
        }

        return _handlerInternal.LoadTrackList(arg);
    }

    public Task PlayQueueTask(IPlayContext? arg) => throw new NotImplementedException();

    public Guid RegisterPositionCallback(ulong minDiff, Action<ulong> callback)
    {
        var newguid = Guid.NewGuid();
        if (_positionCallbacks.TryGetValue(minDiff, out var t))
        {
            t.Item2[newguid] = callback;
        }
        else
        {
            var newDictionary = new Dictionary<Guid, Action<ulong>>
            {
                { newguid, callback }
            };
            _positionCallbacks[minDiff] = (0, newDictionary);
        }

        return newguid;
    }

    public bool UnregisterPositionCallback(Guid callbackId)
    {
        var found = false;
        foreach (var (_, callbacks) in _positionCallbacks)
        {
            if (callbacks.Item2.Remove(callbackId))
            {
                found = true;
            }
        }

        return found;
    }

    private async Task EventsReaderLoop()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                var eventRead = await _handlerInternal.Events.ReadAsync(_cts.Token);

                try
                {
                    switch (eventRead)
                    {
                        case TrackChangedEvent trackChangedEvent:
                            HandleTrackChanged(trackChangedEvent);
                            break;
                        case PausedEvent pausedEvent:
                            HandlePaused(pausedEvent);
                            break;
                        case ResumedEvent resumedEvent:
                            HandleResumed(resumedEvent);
                            break;
                        case SeekedEvent seekedEvent:
                            HandleSeeked(seekedEvent);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "An error occured while processing events.");
                }
            }
        }
        finally
        {
            _cts.Dispose();
        }
    }

    private void HandleSeeked(SeekedEvent seekedEvent) =>
        RxApp.MainThreadScheduler.Schedule(() =>
        {
            SeekTo(seekedEvent.SeekedToMs);
            _waitForSeekResponse.Set();
        });

    private void HandleResumed(ResumedEvent resumedEvent) =>
        RxApp.MainThreadScheduler.Schedule(() =>
        {
            _positionTimespan.Change(0, INCREMENT_BY_MS);
            SeekTo((ulong)_handlerInternal.Position.TotalMilliseconds);
            Paused = false;
        });

    private void HandlePaused(PausedEvent pausedEvent) =>
        RxApp.MainThreadScheduler.Schedule(() =>
        {
            _positionTimespan.Change(Timeout.Infinite, Timeout.Infinite);
            SeekTo((ulong)_handlerInternal.Position.TotalMilliseconds);
            Paused = true;
        });

    private void HandleTrackChanged(TrackChangedEvent trackChangedEvent) =>
        RxApp.MainThreadScheduler.Schedule(() =>
        {
            PlayingItem = trackChangedEvent.Track;
            SeekTo((ulong)_handlerInternal.Position.TotalMilliseconds);
        });

    private void IncrementPosition(object? state) =>
        //check to see if we reached a threshold
        SeekTo(_positionMs + INCREMENT_BY_MS);

    private void SeekTo(ulong pos)
    {
        lock (_positionLock)
        {
            //compare old value to new value
            _positionMs = pos;
            //try to get the callbacks for this diff (it is a >=)
            foreach (var (mindiff, callbacks) in _positionCallbacks)
            {
                var prev = callbacks.prev;
                var diff = _positionMs - prev;
                if (diff >= mindiff)
                {
                    foreach (var (_, callback) in callbacks.Item2)
                    {
                        RxApp.MainThreadScheduler.Schedule(() =>
                        {
                            callback(_positionMs);
                        });
                    }
                }
            }
        }
    }

    public Task Seek(double position)
    {
        _handlerInternal.Seek(position);
        return _waitForSeekResponse.WaitAsync();
    }

    [RelayCommand]
    private void ExpandCoverImage(bool expand)
    {
        CurrentUser.User.UserData.SidebarExpanded = expand;
    }

    [RelayCommand]
    public async Task PauseResume()
    {
        if (Paused)
        {
            await _handlerInternal.Resume();
        }
        else
        {
            await _handlerInternal.Pause();
        }
    }
    public static PlayerViewModel Instance
    {
        get;
        private set;
    }

    public RelayCommand<IDescriptionaryItem> NavigateTo
    {
        get;
    }

}