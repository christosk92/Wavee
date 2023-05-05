using Wavee.Player.Context;
using Wavee.Player.Playback;

namespace Wavee.Player;

public interface IWaveePlayer
{
    Task<Unit> Command(IWaveePlayerCommand command);


    Option<IPlayContext> PlayContext { get; }
    bool PlaybackIsHappening { get; }
    bool IsPaused { get; }

    bool IsShuffling { get; }
    RepeatState RepeatState { get; }
    Option<TimeSpan> CurrentPosition { get; }
    Option<PlayingItem> CurrentItem { get; }

    IObservable<Option<IPlayContext>> PlayContextChanged { get; }
    IObservable<Option<PlayingItem>> CurrentItemChanged { get; }
    /// <summary>
    /// Note: This only changes when the user seeks.
    /// This does not change when the player is playing.
    /// The developer is responsible for keeping track of the current position using a custom timer.
    /// </summary>
    IObservable<Option<TimeSpan>> CurrentPositionChanged { get; }
    IObservable<bool> IsPausedChanged { get; }
    IObservable<bool> PlaybackIsHappeningChanged { get; }

    IObservable<bool> IsShufflingChanged { get; }
    IObservable<RepeatState> RepeatStateChanged { get; }
}

public readonly record struct PlayingItem(
    IPlaybackItem Item,
    PlaybackReasonType Provider,
    Option<int> Index);

public enum PlaybackReasonType
{
    Context,
    Queue
}