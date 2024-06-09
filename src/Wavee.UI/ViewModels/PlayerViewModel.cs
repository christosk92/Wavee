using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using Wavee.Contracts.Interfaces;
using Wavee.Contracts.Interfaces.Contracts;
using Wavee.UI.WinUI;

namespace Wavee.UI.ViewModels;

public sealed partial class PlayerViewModel : ReactiveObject
{
    [AutoNotify] private TimeSpan _position;
    [AutoNotify] private bool _canSeek;
    [AutoNotify] private PlayingItemViewModel? _playingItem;
    public PlayerViewModel()
    {
        //TODO:
        IObservable<bool> canPlayPause = Observable.Return(false);
        IObservable<bool> canPrevious = Observable.Return(false);
        IObservable<bool> canNext = Observable.Return(false);
        IObservable<bool> canRepeat = Observable.Return(false);
        IObservable<bool> canShuffle = Observable.Return(false);
        IObservable<bool> canSeek = Observable.Return(false);
        IObservable<bool> hasLyrics = Observable.Return(false);
        IObservable<bool> canAddToPlaylist = Observable.Return(false);
        IObservable<bool> canHeart = Observable.Return(false);

        canSeek.ToProperty(this, x => x.CanSeek);

        PlayPauseCommand = ReactiveCommand.CreateFromTask(PlayPause, canPlayPause);
        PreviousCommand = ReactiveCommand.CreateFromTask(Previous, canPrevious);
        NextCommand = ReactiveCommand.CreateFromTask(Next, canNext);
        ShuffleCommand = ReactiveCommand.CreateFromTask(Shuffle, canShuffle);
        RepeatCommand = ReactiveCommand.CreateFromTask(Repeat, canRepeat);

        ShowLyricsCommand = ReactiveCommand.Create(ShowLyrics, hasLyrics);
        HeartItemCommand = ReactiveCommand.CreateFromTask<IHeartableItem, Unit>(HeartItem, canHeart);
        AddToPlaylistCommand = ReactiveCommand.CreateFromTask<IAddableToPlaylistItem, Unit>(AddToPlaylist, canAddToPlaylist);
    }

    private Task<Unit> HeartItem(IHeartableItem item, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    private Task<Unit> AddToPlaylist(IAddableToPlaylistItem item, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    private void ShowLyrics()
    {

    }

    private Task Repeat(CancellationToken arg)
    {
        return Task.FromResult(Unit.Default);
    }

    private Task Shuffle(CancellationToken arg)
    {
        return Task.FromResult(Unit.Default);
    }

    private Task<Unit> PlayPause(CancellationToken ct)
    {
        return Task.FromResult(Unit.Default);
    }
    private Task<Unit> Previous(CancellationToken ct)
    {
        return Task.FromResult(Unit.Default);
    }
    private Task<Unit> Next(CancellationToken ct)
    {
        return Task.FromResult(Unit.Default);
    }

    public ReactiveCommand<Unit, Unit> PlayPauseCommand { get; }
    public ReactiveCommand<Unit, Unit> PreviousCommand { get; }
    public ReactiveCommand<Unit, Unit> NextCommand { get; }
    public ReactiveCommand<Unit, Unit> ShuffleCommand { get; }
    public ReactiveCommand<Unit, Unit> RepeatCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowLyricsCommand { get; }
    public ReactiveCommand<IHeartableItem, Unit> HeartItemCommand { get; }
    public ReactiveCommand<IAddableToPlaylistItem, Unit> AddToPlaylistCommand { get; }
}

public record PlayingItemViewModel(IPlayableItem Item, IItem Context);