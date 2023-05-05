using System.Reactive.Linq;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using ReactiveUI;
using Wavee.Player;
using Wavee.Player.Playback;

namespace Wavee.UI.Playback.ViewModels;

public sealed class PlaybackViewModel : ReactiveObject
{
    private bool _playbackIsHappening;
    private bool _isPaused;
    private TimeSpan? _currentPosition;
    private IPlaybackItem? _currentItem;

    private readonly IWaveePlayer _player;
    public PlaybackViewModel(IWaveePlayer player)
    {
        _player = player;

        _player.CurrentItemChanged
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(x => CurrentItem = x.IsSome ? x.ValueUnsafe().Item : null);

        _player.CurrentPositionChanged
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(x => CurrentPosition = x.IsSome ? x.ValueUnsafe() : null);

        _player.IsPausedChanged
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(x => IsPaused = x);

        _player.PlaybackIsHappeningChanged
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(x =>
            {
                PlaybackIsHappening = x;
                if (!x)
                {
                    CurrentItem = null;
                    CurrentPosition = null;
                }
            });
    }

    public bool PlaybackIsHappening
    {
        get => _playbackIsHappening;
        set => this.RaiseAndSetIfChanged(ref _playbackIsHappening, value);
    }

    public bool IsPaused
    {
        get => _isPaused;
        set => this.RaiseAndSetIfChanged(ref _isPaused, value);
    }

    public TimeSpan? CurrentPosition
    {
        get => _currentPosition;
        set => this.RaiseAndSetIfChanged(ref _currentPosition, value);
    }

    public IPlaybackItem? CurrentItem
    {
        get => _currentItem;
        set => this.RaiseAndSetIfChanged(ref _currentItem, value);
    }
}