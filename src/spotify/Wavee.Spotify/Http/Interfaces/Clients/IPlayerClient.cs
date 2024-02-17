using Eum.Spotify.connectstate;
using Wavee.Spotify.Models.Response;

namespace Wavee.Spotify.Http.Interfaces.Clients;

public interface IPlayerClient
{
    /// <summary>
    /// Connect the client with spotify remote control.
    /// </summary>
    /// <param name="deviceName">
    /// The display name of the device, which is shown in the Spotify Connect list.
    /// </param>
    /// <param name="deviceType">
    /// The type of the device, which is shown in the Spotify Connect list.
    /// </param>
    /// <param name="cancel">
    ///  The cancellation-token to allow to cancel the request.
    /// </param>
    /// <returns>
    /// A <see cref="SpotifyPrivateDevice"/> object which can be used to control the device and view active playback state.
    /// </returns>
    ValueTask<SpotifyPrivateDevice> Connect(string deviceName, DeviceType deviceType, CancellationToken cancel = default);

    /// <summary>
    /// Get the object currently being played on the user’s Spotify account.
    /// </summary>
    /// <param name="cancel">The cancellation-token to allow to cancel the request.</param>
    /// <remarks>
    /// https://developer.spotify.com/documentation/web-api/reference-beta/#endpoint-get-the-users-currently-playing-track
    /// </remarks>
    /// <returns>
    /// A <see cref="CurrentlyPlaying"/> object which contains the currently playing track and other metadata about the playback.
    /// </returns>
    Task<SpotifyCurrentlyPlaying> GetCurrentlyPlaying(CancellationToken cancel = default);
}