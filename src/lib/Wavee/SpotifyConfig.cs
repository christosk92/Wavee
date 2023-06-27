using System.Globalization;
using Eum.Spotify.connectstate;
using LanguageExt;
using Wavee.Time.Live;
using static LanguageExt.Prelude;

namespace Wavee;

public sealed class SpotifyConfig
{
    public SpotifyConfig(SpotifyRemoteConfig Remote, SpotifyCacheConfig Cache, SpotifyPlaybackConfig Playback, 
        SpotifyTimeConfig Time,
        CultureInfo Locale)
    {
        this.Remote = Remote;
        this.Cache = Cache;
        this.Playback = Playback;
        this.Time = Time;
        this.Locale = Locale;
    }

    public SpotifyCacheConfig Cache { get; }
    public SpotifyRemoteConfig Remote { get; }
    public SpotifyPlaybackConfig Playback { get; }
    public CultureInfo Locale { get; set; }
    public SpotifyTimeConfig Time { get; set; }
}

public record SpotifyTimeConfig(TimeSyncMethod Method, Option<int> ManualCorrection);
public sealed class SpotifyPlaybackConfig
{
    private Ref<Option<TimeSpan>> _crossfadeDurationRef;

    public SpotifyPlaybackConfig(PreferedQuality preferedQuality, Option<TimeSpan> crossfadeDuration)
    {
        PreferedQuality = preferedQuality;
        _crossfadeDurationRef = Ref(crossfadeDuration);
    }

    public PreferedQuality PreferedQuality { get; set; }
    public Option<TimeSpan> CrossfadeDuration
    {
        get => _crossfadeDurationRef.Value;
        set { atomic(() => _crossfadeDurationRef.Swap(_ => value)); }
    }

    internal Ref<Option<TimeSpan>> CrossfadeDurationRef => _crossfadeDurationRef;
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