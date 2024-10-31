using Eum.Spotify.connectstate;

namespace Wavee.Config;

/// <summary>
/// Represents the configuration settings for Spotify playback.
/// </summary>
public sealed class SpotifyPlaybackConfig
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SpotifyPlaybackConfig"/> class with specified device name and type.
    /// </summary>
    /// <param name="deviceName">The name of the device used for playback.</param>
    /// <param name="deviceType">The type of the device used for playback.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="deviceName"/> is <c>null</c> or empty.
    /// </exception>
    public SpotifyPlaybackConfig(string deviceName, DeviceType deviceType)
    {
        if (string.IsNullOrWhiteSpace(deviceName))
            throw new ArgumentNullException(nameof(deviceName), "Device name cannot be null or empty.");

        DeviceName = deviceName;
        DeviceType = deviceType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpotifyPlaybackConfig"/> class with default settings.
    /// </summary>
    public SpotifyPlaybackConfig()
    {
    }

    /// <summary>
    /// Gets the unique identifier of the device used for playback.
    /// </summary>
    public string DeviceId { get; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets or sets the name of the device used for playback.
    /// </summary>
    public string DeviceName { get; internal set; } = "Wavee";

    /// <summary>
    /// Gets or sets the type of the device used for playback.
    /// </summary>
    public DeviceType DeviceType { get; internal set; } = DeviceType.Computer;

    public TimeSpan? CrossfadeDuration { get; set; } = TimeSpan.FromSeconds(10);

    public TimeSpan? FadeDuration { get; set; } = TimeSpan.FromMilliseconds(40);

    /// <summary>
    /// Gets or sets the maximum number of active nodes.
    /// The higher the number, the more cache nodes will be active at the same time.
    /// However, this will also increase the memory usage.
    /// </summary>
    public int MaxActiveNodes { get; set; } = 10;
}