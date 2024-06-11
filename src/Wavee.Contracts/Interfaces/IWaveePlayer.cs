using Wavee.Contracts.Enums;
using Wavee.Contracts.Interfaces.Playback;

namespace Wavee.Contracts.Interfaces;

public interface IWaveePlayer
{
    /// <summary>
    /// Acquires a lock on the player.
    ///
    /// While the lock is held, no adjustments to the player state can be made.
    /// If the track is playing, it will continue to play but once the track ends, the player will stop until the lock is released.
    /// </summary>
    /// <returns>
    /// A disposable object that once disposed will release the lock.
    /// </returns>
    IDisposable Lock();

    /// <summary>
    /// Clears the entire player state to its default values.
    /// Playback will stop and the queue will be cleared.
    /// Context will be set to null.
    /// Repeat mode will be set to None.
    /// Shuffle mode will be set to false.
    /// </summary>
    void Clear();

    /// <summary>
    /// Sets the shuffle mode of the player.
    /// </summary>
    /// <param name="shuffling">A boolean value indicating if shuffle mode should be enabled or disabled.</param>
    void SetShuffle(bool shuffling);

    /// <summary>
    /// Sets the repeat mode for the player.
    /// </summary>
    /// <param name="repeatContext">The repeat mode to set.</param>
    void SetRepeat(RepeatMode repeatContext);

    /// <summary>
    /// Enqueues a track to be played immediatly by the player.
    /// </summary>
    /// <param name="trackTask">The task that represents the loading of the track's media source.</param>
    /// <param name="position">The starting position of the track.</param>
    /// <param name="startPlayback">A flag indicating whether playback should start immediately.</param>    
    void Play(Task<IMediaSource> trackTask, TimeSpan position, bool startPlayback);

    /// <summary>
    /// Sets the queue for the player.
    /// </summary>
    /// <param name="queue">
    /// The queue to set.
    /// </param>
    void SetQueue(IPlayQueue queue);

    /// <summary>
    /// Sets the play context for the player.
    /// </summary>
    /// <param name="context">The play context to set.</param>
    void SetContext(IPlayContext context);
}