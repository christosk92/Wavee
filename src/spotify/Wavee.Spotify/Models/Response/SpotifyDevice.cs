using Eum.Spotify.connectstate;

namespace Wavee.Spotify.Models.Response;

/// <summary>
/// A struct representing a device that is connected to Spotify Remote.
/// </summary>
/// <param name="Id">
/// The unique identifier of the device.
/// </param>
/// <param name="Name">
/// The name of the device.
/// </param>
/// <param name="Type">
/// The type of the device.
/// </param>
/// <param name="Volume">
/// The volume of the device.
///
/// If the device does not support volume control, this value will be null.
/// </param>
public readonly record struct SpotifyDevice(string Id, string Name, DeviceType Type, float? Volume);