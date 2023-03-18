using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using ReactiveUI;
using Wavee.Enums;
using Wavee.Interfaces.Models;
using Wavee.Models;
using Wavee.UI.Interfaces.Playback;
using Wavee.UI.Playback.PlayerHandlers;

namespace Wavee.UI.ViewModels.Playback
{
    public partial class PlaybackViewModel : ObservableObject
    {
        private const ushort INCREMENT_BY_MS = 50;

        private readonly ConcurrentDictionary<ulong, (ulong prev, Dictionary<Guid, Action<ulong>>)> _positionCallbacks =
            new();

        private readonly CancellationTokenSource _cts;
        private readonly PlayerViewHandlerInternal _handlerInternal;
        private readonly object _positionLock = new();
        private readonly Timer _positionTimespan;
        private ulong _positionMs;
        private readonly AsyncAutoResetEvent _waitForSeekResponse = new();

        public ICommand ExpandCoverImageCommand { get; init; }
        public PlaybackViewModel(ServiceType forService)
        {
            _handlerInternal = forService switch
            {
                ServiceType.Local => Ioc.Default.GetRequiredService<LocalPlayerHandler>(),
                ServiceType.Spotify => null,
                _ => throw new ArgumentOutOfRangeException(nameof(forService), forService, null)
            };
            _positionTimespan = new Timer(IncrementPosition, null, Timeout.Infinite, Timeout.Infinite);
            _cts = new CancellationTokenSource();
            Task.Factory.StartNew(EventsReaderLoop, TaskCreationOptions.LongRunning);
            Instance = this;
        }

        [ObservableProperty]
        private IPlayableItem? _playingItem;

        [ObservableProperty]
        private bool _paused;


        [RelayCommand]
        public Task PlayTask(IPlayContext? arg)
        {
            if (arg == null)
            {
                return Task.CompletedTask;
            }

            return _handlerInternal.LoadTrackList(arg);
        }

        [RelayCommand]
        public void PauseResume()
        {

        }


        [RelayCommand]
        public void NavigateTo(DescriptionItem item)
        {

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
                        Debug.WriteLine($"An error occured while processing events: {ex.Message}");
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
                PauseChanged?.Invoke(this, false);
            });

        private void HandlePaused(PausedEvent pausedEvent) =>
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                _positionTimespan.Change(Timeout.Infinite, Timeout.Infinite);
                SeekTo((ulong)_handlerInternal.Position.TotalMilliseconds);
                Paused = true;
                PauseChanged?.Invoke(this, true);
            });

        private void HandleTrackChanged(TrackChangedEvent trackChangedEvent) =>
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                PlayingItem = trackChangedEvent.Track;
                SeekTo((ulong)_handlerInternal.Position.TotalMilliseconds);
                PlayingItemChanged?.Invoke(this, PlayingItem);
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

        public event EventHandler<IPlayableItem?> PlayingItemChanged;
        public event EventHandler<bool> PauseChanged;
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
        public static PlaybackViewModel? Instance { get; private set; }
    }
}
