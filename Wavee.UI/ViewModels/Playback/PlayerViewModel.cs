using System.Collections.Concurrent;
using CommunityToolkit.Mvvm.ComponentModel;
using Wavee.UI.Identity.Users.Contracts;
using Wavee.UI.Utils;
using Wavee.UI.ViewModels.Playback.Impl;
using Wavee.UI.ViewModels.Playback.PlayerEvents;

namespace Wavee.UI.ViewModels.Playback;

public partial class PlayerViewModel : ObservableRecipient, IDisposable
{
    private const ushort INCREMENT_BY_MS = 50;

    private readonly CancellationTokenSource _cts;
    private readonly PlayerViewHandlerInternal _handlerInternal;
    private readonly Timer _positionTimespan;

    private ConcurrentDictionary<ulong, Dictionary<Guid, Action<ulong>>> _positionCallbacks = new();

    // [ObservableProperty]
    private ulong _positionMs;
    private readonly object _positionLock = new object();

    private readonly IUiDispatcher _uiDispatcher;

    public PlayerViewModel(ServiceType userServiceType, IUiDispatcher uiDispatcher)
    {
        _uiDispatcher = uiDispatcher;
        _handlerInternal = userServiceType switch
        {
            ServiceType.Local => new LocalPlayerHandler(),
            ServiceType.Spotify => new SpotifyPlayerHandler(),
            _ => throw new ArgumentOutOfRangeException(nameof(userServiceType), userServiceType, null)
        };
        _positionTimespan = new Timer(IncrementPosition, null, Timeout.Infinite, Timeout.Infinite);
        _cts = new CancellationTokenSource();
        Task.Factory.StartNew(EventsReaderLoop, TaskCreationOptions.LongRunning);
    }

    public Guid RegisterPositionCallback(ulong minDiff, Action<ulong> callback)
    {
        var newguid = Guid.NewGuid();
        if (_positionCallbacks.TryGetValue(minDiff, out var t))
        {
            t[newguid] = callback;
        }
        else
        {
            _positionCallbacks[minDiff] = new Dictionary<Guid, Action<ulong>>
            {
                { newguid, callback }
            };
        }

        return newguid;
    }

    public bool UnregisterPositionCallback(Guid callbackId)
    {
        var found = false;
        foreach (var (_, callbacks) in _positionCallbacks)
        {
            if (callbacks.Remove(callbackId))
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
        }
        finally
        {
            _cts.Dispose();
        }
    }

    private void HandleSeeked(SeekedEvent seekedEvent)
    {
        lock (_positionLock)
        {
            _positionMs = seekedEvent.SeekedToMs;
        }
    }

    private void HandleResumed(ResumedEvent resumedEvent)
    {
        _positionTimespan.Change(0, INCREMENT_BY_MS);
        //TODO: Resync position
    }

    private void HandlePaused(PausedEvent pausedEvent)
    {
        _positionTimespan.Change(Timeout.Infinite, Timeout.Infinite);
        //TODO: Resync position
    }

    private void HandleTrackChanged(TrackChangedEvent trackChangedEvent)
    {
    }

    private void IncrementPosition(object? state)
    {
        //check to see if we reached a threshold
        lock (_positionLock)
        {
            //compare old value to new value
            var old = _positionMs;
            _positionMs += INCREMENT_BY_MS;
            var diff = _positionMs - old;
            //try to get the callbacks for this diff (it is a >=)
            foreach (var (mindiff, callbacks) in _positionCallbacks)
            {
                if (diff >= mindiff)
                {
                    foreach (var (_, callback) in callbacks)
                    {
                        _uiDispatcher.Dispatch(DispatcherQueuePriority.High,
                            () => callback(_positionMs));
                    }
                }
            }
        }
    }

    public void Dispose()
    {
        _handlerInternal.Dispose();
    }
}
