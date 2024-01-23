using LanguageExt;

namespace Wavee.Contexting;

public interface IWaveePlayerContext
{
    /// <summary>
    /// Get the next stream in the context
    /// </summary>
    /// <returns>
    /// The next stream in the context or None if there is no next stream
    ///
    /// Note that if auto-play is enabled, the next stream will not be fetched.
    /// Rather the caller should instantiate a new auto-play context and play it.
    /// </returns>
    ValueTask<Option<WaveeContextStream>> GetNextStream();

    /// <summary>
    /// Get the previous stream in the context
    /// </summary>
    /// <returns>
    /// The previous stream in the context or None if there is no previous stream
    /// </returns>
    ValueTask<Option<WaveeContextStream>> GetPreviousStream();

    /// <summary>
    /// Get the current stream in the context
    /// </summary>
    Option<WaveeContextStream> CurrentStream { get; }

    /// <summary>
    /// Skip number of streams in the context without fetching them necessarily
    /// </summary>
    /// <param name="count">
    /// Number of streams to skip
    /// 
    /// <returns>
    ///  True if the skip was successful, false otherwise
    /// </returns>
    /// </param>
    /// <param name="skippedCount">
    /// Number of streams that were skipped. This is always 0 if the skip was unsuccessful
    /// </param>
    ValueTask<bool> TrySkip(int count);

    ValueTask<bool> MoveTo(int absoluteIndex);
}