using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Reactive.Concurrency;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using ReactiveUI;
using Splat;
using Wavee.Enums;
using Wavee.Interfaces.Models;
using Wavee.Models;
using Wavee.UI.Interfaces.Playback;
using Wavee.UI.Playback.PlayerHandlers;
using Wavee.UI.ViewModels.Libray;
using Wavee.UI.ViewModels.Shell;
using Wavee.UI.ViewModels.User;
using static CommunityToolkit.Mvvm.ComponentModel.__Internals.__TaskExtensions.TaskAwaitableWithoutEndValidation;

namespace Wavee.UI.ViewModels.Playback
{
    public partial class PlaybackViewModel : ObservableObject
    {
        private const string playback_shuffle_pref_name = "playback.shuffle";
        private const string playback_repeatstate_pref_name = "playback.repeatstate";
        private const string playback_item_pref_name = "playback.lastitem.index";
        private const string playback_item_position_pref_name = "playback.lastitem.position";
        private const string playback_context_pref_name = "playback.lastcontext";
        private const string playback_volume_pref_name = "playback.volume";

        private const ushort INCREMENT_BY_MS = 50;

        private readonly ConcurrentDictionary<ulong, (ulong prev, Dictionary<Guid, Action<ulong>>)> _positionCallbacks =
            new();

        private readonly CancellationTokenSource _cts;
        private readonly PlayerViewHandlerInternal _handlerInternal;
        private readonly object _positionLock = new();
        private readonly Timer _positionTimespan;
        private ulong _positionMs;
        private readonly AsyncAutoResetEvent _waitForSeekResponse = new();

        public ICommand ExpandCoverImageCommand
        {
            get;
            init;
        }

        private readonly ILogger<PlaybackViewModel>? _logger;
        private readonly UserViewModel _user;
        public PlaybackViewModel(UserViewModel user, ILogger<PlaybackViewModel>? logger)
        {
            _user = user;
            _logger = logger;

            _handlerInternal = user.ForProfile.ServiceType switch
            {
                ServiceType.Local => Ioc.Default.GetRequiredService<LocalPlayerHandler>(),
                ServiceType.Spotify => null,
                _ => throw new ArgumentOutOfRangeException(nameof(user.ForProfile.ServiceType), user.ForProfile.ServiceType, null)
            };
            _positionTimespan = new Timer(IncrementPosition, null, Timeout.Infinite, Timeout.Infinite);
            _cts = new CancellationTokenSource();

            Task.Factory.StartNew(EventsReaderLoop, TaskCreationOptions.LongRunning);
            Instance = this;
        }

        [ObservableProperty] private IPlayableItem? _playingItem;

        [ObservableProperty] private bool _paused = true;

        [ObservableProperty] private RepeatState _repeatState;

        [ObservableProperty] private bool _shuffle;

        [ObservableProperty] private double _volume;

        [ObservableProperty] private IPlayContext? _currentContext;


        public Task Play(IPlayContext? arg, int index)
        {
            if (arg == null)
            {
                return Task.CompletedTask;
            }

            return _handlerInternal.LoadTrackList(arg, index);
        }

        [RelayCommand]
        public async Task PauseResume()
        {
            if (_handlerInternal.Paused)
            {
                await _handlerInternal.Resume();
            }
            else
            {
                await _handlerInternal.Pause();
            }
        }

        [RelayCommand]
        public async Task SkipNext()
        {
            await _handlerInternal.SkipNext();
        }

        [RelayCommand]
        public async Task SkipPrev()
        {
            await _handlerInternal.SkipPrevious();
        }

        [RelayCommand]
        public async Task ToggleRepeat()
        {
            await _handlerInternal.GoNextRepeatState();
        }

        [RelayCommand]
        public async Task ToggleShuffle()
        {
            await _handlerInternal.ToggleShuffle();
        }


        [RelayCommand]
        public void NavigateTo(DescriptionItem item)
        {
        }

        private async Task EventsReaderLoop()
        {
            try
            {
                //toggle  shuffle etc, if this fails just ignore
                try
                {
                    await _handlerInternal.GoShuffle(_user.ReadPreference<bool>(playback_shuffle_pref_name));
                    await _handlerInternal.GoToRepeatState(
                        _user.ReadPreference<RepeatState>(playback_repeatstate_pref_name));

                    var item = _user.ReadPreference<int?>(playback_item_pref_name);
                    if (item != null)
                    {
                        var context =
                            _user.ReadJsonPreference<IPlayContext?>(playback_context_pref_name);
                        await _handlerInternal.LoadTrackListButDoNotPlay(context, item.Value);
                    }
                    await _handlerInternal.SetVolume(_user.ReadPreference<double>(playback_volume_pref_name, 1d));
                    await _handlerInternal.Seek(_user.ReadPreference<ulong>(playback_item_position_pref_name));

                }
                catch (Exception x)
                {
                    _logger?.LogError(x, "An error occured while trying to set initial state.");
                }

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
                            case ShuffleToggledEvent shuffleToggled:
                                HandleShuffleToggled(shuffleToggled);
                                break;
                            case RepeatStateChangedEvent repeatStateChanged:
                                HandleRepeatStateChange(repeatStateChanged);
                                break;
                            case VolumeChangedEvent volumeChanged:
                                HandleVolumeChange(volumeChanged);
                                break;
                            case ContextChangedEvent contextChanged:
                                HandleContextChanged(contextChanged);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"An error occured while processing events: {ex.Message}");
                    }
                }
            }
            catch (Exception x)
            {
                _logger?.LogError(x, "An error occured in the main event reader loop.");
            }
            finally
            {
                _cts.Dispose();
            }
        }

        private void HandleContextChanged(ContextChangedEvent ctxChanged)
        {
            RxApp.MainThreadScheduler.Schedule(async () =>
            {
                CurrentContext = ctxChanged.context;
                await ShellViewModel.Instance.User.SaveJsonPreference(playback_context_pref_name, CurrentContext);
            });
        }

        private void HandleVolumeChange(VolumeChangedEvent volumeChanged)
        {
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                Volume = volumeChanged.Volume * 100;
                ShellViewModel.Instance.User.SavePreference(playback_volume_pref_name, volumeChanged.Volume);
            });
        }

        private void HandleRepeatStateChange(RepeatStateChangedEvent repeatStateChangedEvent) =>
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                RepeatState = repeatStateChangedEvent.state;
                ShellViewModel.Instance.User.SavePreference(playback_repeatstate_pref_name, RepeatState);
            });

        private void HandleShuffleToggled(ShuffleToggledEvent shuffleToggledEvent) =>
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                Shuffle = shuffleToggledEvent.shuffling;
                ShellViewModel.Instance.User.SavePreference(playback_shuffle_pref_name, Shuffle);
            });

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

        private void HandleTrackChanged(TrackChangedEvent trackChangedEvent)
        {
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                PlayingItem = trackChangedEvent.Track;
                ShellViewModel.Instance.User.SavePreference(playback_item_pref_name, trackChangedEvent.Index);

                SeekTo((ulong)_handlerInternal.Position.TotalMilliseconds);

                ShellViewModel.Instance.User.SavePreference(playback_item_position_pref_name,
                    _handlerInternal.Position.TotalMilliseconds);

                PlayingItemChanged?.Invoke(this, PlayingItem);
                Volume = _handlerInternal.Volume * 100;

                ShellViewModel.Instance.User.SavePreference(playback_volume_pref_name, _handlerInternal.Volume);
            });
        }

        private void IncrementPosition(object? state) =>
            //check to see if we reached a threshold
            SeekTo(_positionMs + INCREMENT_BY_MS);

        private void SeekTo(ulong pos)
        {
            lock (_positionLock)
            {
                //compare old value to new value
                _positionMs = pos;
                bool gotAny = false;
                //try to get the callbacks for this diff (it is a >=)
                foreach (var (mindiff, callbacks) in _positionCallbacks)
                {
                    var prev = callbacks.prev;
                    var diff = _positionMs - prev;
                    if (diff >= mindiff)
                    {
                        gotAny = true;
                        foreach (var (_, callback) in callbacks.Item2)
                        {
                            RxApp.MainThreadScheduler.Schedule(() =>
                            {
                                callback(_positionMs);
                            });
                        }
                    }
                }

                if (gotAny)
                {
                    ShellViewModel.Instance.User.SavePreference(playback_item_position_pref_name, pos);
                }
            }
        }

        public Task Seek(double position)
        {
            _handlerInternal.Seek(position);
            return _waitForSeekResponse.WaitAsync();
        }

        /// <summary>
        /// Set the volume (0 - 100)
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public ValueTask SetVolume(double d)
        {
            return _handlerInternal.SetVolume(d / 100);
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

        public static PlaybackViewModel? Instance
        {
            get;
            private set;
        }
    }
}