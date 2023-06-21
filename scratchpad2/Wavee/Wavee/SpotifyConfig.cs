using Eum.Spotify.connectstate;

namespace Wavee;

public sealed class SpotifyConfig
{
    public SpotifyConfig(SpotifyRemoteConfig Remote)
    {
        this.Remote = Remote;
    }

    public SpotifyRemoteConfig Remote { get; }
}

public sealed class SpotifyRemoteConfig
{
    public SpotifyRemoteConfig(string deviceName, DeviceType deviceType)
    {
        DeviceName = deviceName;
        DeviceType = deviceType;
    }

    public string DeviceName { get; set; }
    public DeviceType DeviceType { get; set; }
}