using Eum.Spotify.connectstate;

namespace Wavee.Spotify;

public class SpotifyConfig
{
    public SpotifyConfig(SpotifyRemoteConfig remote, SpotifyPlaybackConfig playback)
    {
        Remote = remote;
        Playback = playback;
    }

    public SpotifyRemoteConfig Remote { get; }
    public SpotifyPlaybackConfig Playback { get; }
}

public class SpotifyPlaybackConfig
{
}

public class SpotifyRemoteConfig
{
    public SpotifyRemoteConfig(string deviceName, DeviceType deviceType)
    {
        DeviceName = deviceName;
        DeviceType = deviceType;
    }

    public string DeviceName { get; internal set; }
    public DeviceType DeviceType { get; internal set; }
}