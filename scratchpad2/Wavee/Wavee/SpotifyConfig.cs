using Eum.Spotify.connectstate;
using LanguageExt;

namespace Wavee;

public sealed class SpotifyConfig
{
    public SpotifyConfig(SpotifyRemoteConfig Remote, SpotifyCacheConfig Cache, SpotifyPlaybackConfig playback)
    {
        this.Remote = Remote;
        this.Cache = Cache;
        Playback = playback;
    }

    public SpotifyCacheConfig Cache { get; }
    public SpotifyRemoteConfig Remote { get; }
    public SpotifyPlaybackConfig Playback { get; }
}

public sealed class SpotifyPlaybackConfig
{
    public SpotifyPlaybackConfig(PreferedQuality preferedQuality)
    {
        PreferedQuality = preferedQuality;
    }

    public PreferedQuality PreferedQuality { get; set; }
}

public enum PreferedQuality
{
    Normal,
    High,
    Highest,
}

public sealed class SpotifyCacheConfig
{
    public SpotifyCacheConfig(Option<string> CacheLocation, Option<long> MaxCacheSize)
    {
        this.CacheLocation = CacheLocation;
        this.MaxCacheSize = MaxCacheSize;
    }

    public Option<string> CacheLocation { get; set; }
    public Option<long> MaxCacheSize { get; set; }
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