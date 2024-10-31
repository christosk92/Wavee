using System.Reactive;
using Eum.Spotify.connectstate;
using Wavee.Enums;
using Wavee.Exceptions;
using Wavee.Models.Common;
using Wavee.Models.Remote;
using Wavee.Models.Remote.Commands;
using Wavee.Models.Remote.Commands.Play;
using Wavee.Services.Playback;

namespace Wavee.Interfaces;

/// <summary>
/// Interface defining the client for managing Spotify playback.
/// </summary>
public interface ISpotifyPlaybackClient
{
    /// <summary>
    /// Represents an observable stream of Spotify playback states that clients can subscribe to for real-time updates.
    /// </summary>
    /// <remarks>
    /// The <see cref="PlaybackState"/> property exposes an <see cref="IObservable{ISpotifyPlaybackState}"/> which emits events whenever there is a change in the Spotify playback state. This allows consumers to react to playback changes such as play, pause, device switching, or error occurrences in real-time.
    ///
    /// <para>
    /// The observable stream emits instances of classes implementing the <see cref="ISpotifyPlaybackState"/> interface, representing various playback scenarios:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description><see cref="SpotifyLocalPlaybackState"/>: Indicates that playback is occurring on the local device where the client is running.</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="SpotifyRemotePlaybackState"/>: Indicates that playback is occurring on a remote device connected via Spotify Connect.</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="NonePlaybackState"/>: Indicates that there is currently no active playback.</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="ErrorPlaybackState"/>: Indicates that an error has occurred related to playback operations.</description>
    ///   </item>
    /// </list>
    ///
    /// <para>
    /// **Emission Behavior**:
    /// </para>
    /// <list type="number">
    ///   <item>
    ///     <description>
    ///       The observable emits the current playback state immediately upon subscription.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Subsequent changes in playback (e.g., play, pause, device switch) trigger the emission of new <see cref="ISpotifyPlaybackState"/> instances reflecting the updated state.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       In the event of an error within the playback system, an <see cref="ErrorPlaybackState"/> is emitted to provide error details.
    ///     </description>
    ///   </item>
    /// </list>
    ///
    /// <para>
    /// **Threading Considerations**:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       The <see cref="IObservable{ISpotifyPlaybackState}"/> is thread-safe and can emit events from any thread. Subscribers should handle threading appropriately, especially when updating UI elements.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Utilize synchronization contexts or schedulers to marshal events to specific threads as needed.
    ///     </description>
    ///   </item>
    /// </list>
    ///
    /// <para>
    /// **Subscription Lifecycle**:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       Subscribers receive updates for as long as they remain subscribed. Properly dispose of subscriptions to prevent memory leaks.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Use mechanisms such as <c>Dispose()</c> or <c>using</c> statements to manage subscription lifetimes.
    ///     </description>
    ///   </item>
    /// </list>
    ///
    /// <para>
    /// **Usage Example**:
    /// </para>
    /// <code>
    /// // Assuming spotifyClient is an instance of ISpotifyPlaybackClient
    /// var subscription = spotifyClient.PlaybackState.Subscribe(state =>
    /// {
    ///     switch (state)
    ///     {
    ///         case SpotifyLocalPlaybackState localState:
    ///             Console.WriteLine($"Playing locally on device ID: {localState.DeviceId}");
    ///             break;
    ///
    ///         case SpotifyRemotePlaybackState remoteState:
    ///             Console.WriteLine($"Playing on remote device '{remoteState.DeviceName}' (ID: {remoteState.DeviceId})");
    ///             break;
    ///
    ///         case NonePlaybackState _:
    ///             Console.WriteLine("No active playback.");
    ///             break;
    ///
    ///         case ErrorPlaybackState errorState:
    ///             Console.WriteLine($"Playback error: {errorState.ErrorMessage}");
    ///             break;
    ///     }
    /// });
    ///
    /// // To unsubscribe when updates are no longer needed
    /// subscription.Dispose();
    /// </code>
    /// </remarks>
    /// <value>
    /// An <see cref="IObservable{ISpotifyPlaybackState}"/> that emits playback state changes. Subscribers receive updates whenever the Spotify playback state changes, including transitions between local and remote playback, playback pauses, stops, or errors.
    /// </value>
    IObservable<ISpotifyPlaybackState> PlaybackState { get; }


    /// <summary>
    /// Connects to a remote control for Spotify playback on a specified device.
    /// </summary>
    /// <param name="deviceName">The name of the device to connect to. If <c>null</c>, the default device will be used as supplied in the configuration.</param>
    /// <param name="deviceType">The type of the device to connect to. If <c>null</c>, the default device type will be used as supplied in the configuration.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>
    /// A task representing the asynchronous operation, with a result of <see cref="SpotifyDeviceInfo"/> that provides details about the connected device.
    /// </returns>
    /// <exception cref="WaveeNetworkException">
    /// Thrown if the connection to the specified device fails.
    /// </exception>
    Task<Unit> ConnectToRemoteControl(string? deviceName,
        DeviceType? deviceType,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// Plays a specified media item on the current playback device.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task Play(IPlayItemCommandBuilder request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses the current playback.
    /// </summary>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> to monitor for cancellation requests.
    /// </param>
    /// <returns></returns>
    Task Pause(CancellationToken cancellationToken = default);

    /// <summary>
    /// /Resumes playback if it is currently paused.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task Resume(CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeks to a specified position within the currently playing media item.
    /// </summary>
    /// <param name="to"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task Seek(TimeSpan to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Skips to the next track in the playback queue.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SkipNext(CancellationToken cancellationToken = default);

    /// <summary>
    /// Goes back to the previous track in the playback queue.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SkipPrevious(CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggles the shuffle mode of the playback queue.
    /// </summary>
    /// <param name="shuffle"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SetShuffle(bool shuffle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the repeat mode of the playback queue.
    /// </summary>
    /// <param name="repeatMode"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SetRepeatingContext(RepeatMode repeatMode, CancellationToken cancellationToken = default);
}