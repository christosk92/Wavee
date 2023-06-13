using Eum.Spotify.connectstate;
using LanguageExt;

namespace Wavee.Spotify;

public class SpotifyConfig
{
    public SpotifyConfig(SpotifyRemoteConfig remote, SpotifyPlaybackConfig playback, SpotifyCacheConfig cache)
    {
        Remote = remote;
        Playback = playback;
        Cache = cache;
    }

    public SpotifyRemoteConfig Remote { get; }
    public SpotifyPlaybackConfig Playback { get; }
    public SpotifyCacheConfig Cache { get; }
}

public class SpotifyCacheConfig
{
    public SpotifyCacheConfig(Option<string> cacheRoot)
    {
        CacheRoot = cacheRoot;
    }

    public Option<string> CacheRoot { get; set; }
}

public class SpotifyPlaybackConfig
{
    public SpotifyPlaybackConfig(TimeSpan crossfadeDuration, PreferredQualityType preferedQuality)
    {
        PreferedQuality = preferedQuality;
        CrossfadeDuration = crossfadeDuration;
    }

    public Option<TimeSpan> CrossfadeDuration { get; set; }
    public PreferredQualityType PreferedQuality { get; set; }
}

public enum PreferredQualityType
{
    /// <summary>
    /// 96 kbit/s
    /// </summary>
    Normal,

    /// <summary>
    /// 160 kbit/s
    /// </summary>
    High,

    /// <summary>
    /// 320 kbit/s
    /// </summary>
    VeryHigh
}

public class SpotifyRemoteConfig
{
    public SpotifyRemoteConfig(string deviceName, DeviceType deviceType)
    {
        DeviceName = deviceName;
        DeviceType = deviceType;
    }

    public string DeviceName { get; set; }
    public DeviceType DeviceType { get; set; }
}