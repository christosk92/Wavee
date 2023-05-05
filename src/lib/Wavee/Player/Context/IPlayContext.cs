using Wavee.Player.Playback;

namespace Wavee.Player.Context;

/// <summary>
/// Represents a context of items to play.
/// </summary>
public interface IPlayContext
{
    /// <summary>
    /// Open a stream at the given index.
    /// </summary>
    /// <param name="at">
    /// An either with on the left: Random shuffle and on the right maybe an index.
    /// If the right option is None, then the player should play from the beginning.
    /// </param>
    /// <returns>
    /// A stream that can be played, with metadata.
    /// </returns>
    ValueTask<(IPlaybackStream Stream, int AbsoluteIndex)> GetStreamAt(Either<Shuffle, Option<int>> at);

    /// <summary>
    /// Count the number of items in this context.
    /// </summary>
    /// <returns>
    /// Maybe the number of items in this context.
    /// If this is None, then the number of items is unknown (infinite context).
    /// </returns>
    ValueTask<Option<int>> Count();
}

/// <summary>
/// A type of structure that represents a shuffle command.
/// </summary>
public readonly record struct Shuffle;