using Wavee.Config;
using Wavee.Enums;
using Wavee.Playback.Contexts;
using Wavee.Playback.Player;
using Wavee.Playback.Streaming;
using Wavee.Services.Playback;

namespace Wavee.Interfaces;

/// <summary>
/// Defines the contract for the Wavee media player, providing functionalities
/// such as playback control, media item management, volume adjustment, and state monitoring.
/// </summary>
public interface IWaveePlayer
{
    Task Initialize();

    #region Properties

    SpotifyConfig Config { get; set; }

    ITimeProvider TimeProvider { get; set; }

    /// <summary>
    /// Gets an observable sequence that notifies subscribers of changes to the local Spotify playback state.
    /// </summary>
    /// <value>
    /// An <see cref="IObservable{T}"/> emitting <see cref="SpotifyLocalPlaybackState"/> instances or <c>null</c> if unavailable.
    /// </value>
    IObservable<SpotifyLocalPlaybackState?> State { get; }

    /// <summary>
    /// Gets the current volume level of the player.
    /// </summary>
    /// <value>
    /// A <see cref="float"/> representing the volume level, typically ranging from 0.0 (mute) to 1.0 (maximum volume).
    /// </value>
    float Volume { get; }

    /// <summary>
    /// Gets the current playback context of the player, which may include information such as the current playlist, album, or artist.
    /// </summary>
    /// <value>
    /// An instance of <see cref="WaveePlayerPlaybackContext"/> representing the current playback context, or <c>null</c> if no context is set.
    /// </value>
    WaveePlayerPlaybackContext? Context { get; }

    /// <summary>
    /// Gets or sets the delegate responsible for requesting audio streams for specific tracks.
    /// This allows customization of how audio data is retrieved and processed.
    /// </summary>
    /// <value>
    /// A delegate of type <see cref="RequestAudioStreamForTrackAsync"/> that handles audio stream requests, or <c>null</c> if not set.
    /// </value>
    RequestAudioStreamForTrackAsync? RequestAudioStreamForTrack { get; set; }

    /// <summary>
    /// Gets the position of the current media item in the playback queue.
    /// </summary>
    TimeSpan Position { get; }

    /// <summary>
    /// Gets the next N tracks in the queue.
    /// </summary>
    Task<List<WaveePlayerMediaItem>> GetUpcomingTracksAsync(int count, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all previous tracks in the context
    /// </summary>
    Task<List<WaveePlayerMediaItem>> GetPreviousTracksInCOntextAsync(int count, CancellationToken cancellationToken);

    #endregion

    #region Playback Control Methods

    /// <summary>
    /// Stops the current playback immediately.
    /// </summary>
    /// <remarks>
    /// This method halts any ongoing media playback and resets the player's state.
    /// </remarks>
    Task Stop();

    /// <summary>
    /// Pauses the current playback.
    /// </summary>
    /// <remarks>
    /// Temporarily halts playback without resetting the current position, allowing playback to be resumed later.
    /// </remarks>
    Task Pause();

    /// <summary>
    /// Resumes playback if it is currently paused.
    /// </summary>
    /// <remarks>
    /// Continues playback from the current position if the player is in a paused state.
    /// </remarks>
    Task Resume();

    /// <summary>
    /// Seeks to a specified position within the currently playing media item.
    /// </summary>
    /// <param name="to">The <see cref="TimeSpan"/> representing the target position to seek to.</param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous seek operation.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the specified <paramref name="to"/> is outside the bounds of the media item's duration.
    /// </exception>
    Task Seek(TimeSpan to);

    /// <summary>
    /// Adjusts the player's volume to the specified level.
    /// </summary>
    /// <param name="volume">
    /// A <see cref="float"/> representing the desired volume level, typically ranging from 0.0 (mute) to 1.0 (maximum volume).
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the specified <paramref name="volume"/> is outside the acceptable range.
    /// </exception>
    Task SetVolume(float volume);

    /// <summary>
    /// Skips to the next media item in the current playback queue.
    /// </summary>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous skip operation.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if there is no next media item to skip to.
    /// </exception>
    Task SkipNext();

    /// <summary>
    /// Returns to the previous media item in the current playback queue.
    /// </summary>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous skip operation.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if there is no previous media item to skip to.
    /// </exception>
    Task SkipPrevious();

    #endregion

    #region Media Playback Methods

    /// <summary>
    /// Adds a media item to the playback queue.
    /// </summary>
    /// <param name="mediaItem">The media item to be added to the queue.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddToQueue(WaveePlayerMediaItem mediaItem);

    /// <summary>
    /// Asynchronously plays the specified media item starting from a given position within the media.
    /// </summary>
    /// <param name="context">
    ///     An optional <see cref="WaveePlayerPlaybackContext"/> providing additional playback context, such as playlist or album information.
    ///     If <c>null</c>, the current context remains unchanged.
    /// </param>
    /// <param name="mediaItem">
    ///     The <see cref="WaveePlayerMediaItem"/> representing the media to be played.
    /// </param>
    /// <param name="startFrom">
    ///     A <see cref="TimeSpan"/> indicating the position within the media item from which to start playback.
    /// </param>
    /// <param name="overrideShuffling"></param>
    /// <param name="overrideRepeatMode"></param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous play operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="mediaItem"/> is <c>null</c>.
    /// </exception>
    Task PlayMediaItemAsync(WaveePlayerPlaybackContext context,
        WaveePlayerMediaItem mediaItem, TimeSpan startFrom,
        bool? overrideShuffling = null,
        RepeatMode? overrideRepeatMode = null);

    /// <summary>
    /// Asynchronously plays a media item based on its position within a specified context.
    /// </summary>
    /// <param name="context">
    /// The <see cref="WaveePlayerPlaybackContext"/> representing the playback context, such as a playlist or album.
    /// </param>
    /// <param name="pageIndex">
    /// An <see cref="int"/> indicating the page index within the context where the media item is located.
    /// </param>
    /// <param name="trackIndex">
    /// An <see cref="int"/> specifying the index of the track within the specified page to be played.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous play operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="context"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="pageIndex"/> or <paramref name="trackIndex"/> is outside the valid range for the specified context.
    /// </exception>
    Task PlayMediaItemAsync(WaveePlayerPlaybackContext context, int pageIndex, int trackIndex);

    Task SetShuffle(bool value);
    Task SetRepeatMode(RepeatMode mode);

    #endregion
}

public delegate Task<AudioStream?> RequestAudioStreamForTrackAsync(WaveePlayerMediaItem itemId,
    CancellationToken cancellationToken);