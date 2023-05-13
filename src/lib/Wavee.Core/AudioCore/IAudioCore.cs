using Wavee.Core.Contracts;
using Wavee.Core.Id;

namespace Wavee.Core.AudioCore;

/// <summary>
/// Any client that implements this interface can be used to query for audio items,
/// play audio items, and control playback. 
/// </summary>
public interface IAudioCore
{
    /// <summary>
    /// The unique identifier for the audio core.
    /// This should be a never-changing value, and is recommended to be a hardcoded static value.
    /// This is used to categorize audio cores and <see cref="AudioId"/>'s.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Gets the audio item with the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="id">
    /// The unique identifier for the audio item on the source.
    /// </param>
    /// <param name="ct">
    /// The cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    ///  A <see cref="ValueTask{ITrack}"/> representing the asynchronous operation with the result being the audio item.
    /// </returns>
    ValueTask<ITrack> GetTrackAsync(string id, CancellationToken ct = default);
}