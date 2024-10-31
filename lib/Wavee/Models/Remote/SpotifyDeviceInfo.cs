using Eum.Spotify.connectstate;

namespace Wavee.Models.Remote;

/// <summary>
/// Represents information about a Spotify-connected device.
/// </summary>
public class SpotifyDeviceInfo
{
    /// <summary>
    /// The unique identifier of the device.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// The name of the device.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The type of the device.
    /// </summary>
    public DeviceType Type { get; }

    /// <summary>
    /// Indicates whether the device is controlled by the current client.
    /// </summary>
    public bool IsOurDevice { get; }

    /// <summary>
    /// The current volume level of the device (0.0 to 1.0).
    /// </summary>
    public double Volume { get; }

    /// <summary>
    /// Indicates whether the client can control the device's volume.
    /// </summary>
    public bool CanControlVolume { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpotifyDeviceInfo"/> record.
    /// </summary>
    /// <param name="Id">The unique identifier of the device.</param>
    /// <param name="Name">The name of the device.</param>
    /// <param name="Type">The type of the device.</param>
    /// <param name="IsOurDevice">Indicates whether the device is controlled by the current client.</param>
    /// <param name="Volume">The current volume level of the device (0.0 to 1.0).</param>
    /// <param name="CanControlVolume">Indicates whether the client can control the device's volume.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="Id"/> or <paramref name="Name"/> is <c>null</c> or empty.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="Volume"/> is not between 0.0 and 1.0.
    /// </exception>
    public SpotifyDeviceInfo(
        string Id,
        string Name,
        DeviceType Type,
        bool IsOurDevice,
        double Volume,
        bool CanControlVolume)
    {
        if (string.IsNullOrWhiteSpace(Id))
            throw new ArgumentNullException(nameof(Id), "Device ID cannot be null or empty.");

        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentNullException(nameof(Name), "Device name cannot be null or empty.");

        if (Volume < 0.0 || Volume > 1.0)
            throw new ArgumentOutOfRangeException(nameof(Volume), "Volume must be between 0.0 and 1.0.");
        this.Id = Id;
        this.Name = Name;
        this.Type = Type;
        this.IsOurDevice = IsOurDevice;
        this.Volume = Volume;
        this.CanControlVolume = CanControlVolume;
    }
}