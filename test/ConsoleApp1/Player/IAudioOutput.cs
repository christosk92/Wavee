using Wavee.Playback.Player;

namespace ConsoleApp1.Player;

/// <summary>
/// Defines the contract for audio output implementations.
/// </summary>
internal interface IAudioOutput : IDisposable
{
    /// <summary>
    /// Gets the maximum number of active cache entries.
    /// </summary>
    int MaxCacheEntries { get; }

    /// <summary>
    /// Event triggered when media playback has ended.
    /// </summary>
    event EventHandler<WaveePlaybackStreamEndedArgs>? MediaEnded;

    event EventHandler? PrefetchRequested;
    
    /// <summary>
    /// Plays the specified WaveePlaybackStream.
    /// </summary>
    /// <param name="playbackStream">The playback stream to play.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task Play(WaveePlaybackStream playbackStream, CancellationToken cancellationToken);

    /// <summary>
    /// Stops any currently playing media.
    /// </summary>
    void Stop();

    /// <summary>
    /// Seeks to the specified position within the current media stream.
    /// </summary>
    /// <param name="position">The target position to seek to.</param>
    void Seek(TimeSpan position);

    /// <summary>
    /// Gets the current playback position.
    /// </summary>
    TimeSpan Position { get; }

    WaveePlaybackStream? CurrentPlaybackStream { get; }
    bool IsPaused { get; }
    void Resume();
    void Pause();
    void SetVolume(float volume);
    
    float Volume { get; }
}