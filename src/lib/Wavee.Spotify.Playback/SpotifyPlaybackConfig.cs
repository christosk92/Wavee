using Eum.Spotify.connectstate;

namespace Wavee.Spotify.Playback;

/// <summary>
/// A type of config to help with playback.
/// </summary>
/// <param name="DeviceName">The name of the device which Wavee should report to Spotify.</param>
/// <param name="DeviceType">The type of device which Wavee should report to Spotify.</param>
/// <param name="InitialVolume">The initial volume of the device.</param>
/// <param name="MaxVolume">The maximum volume of the device. Most users should just leave this as default. As most Spotify clients assume ushort::max.</param>
/// <param name="VolumeSteps">Useful for mobile, where the volume button increases the volume by this amount. Most users shoud leave this as 64.</param>
public record SpotifyPlaybackConfig(
    string DeviceName,
    DeviceType DeviceType,
    PreferredQualityType PreferredQuality,
    ushort InitialVolume,
    ushort MaxVolume = ushort.MaxValue,
    ushort VolumeSteps = 64);

public enum PreferredQualityType
{
    Highest,
    High,
    Default,
    Low,
}