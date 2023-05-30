using Eum.Spotify.connectstate;
using LanguageExt;
using Wavee.Spotify.Infrastructure.Playback;

namespace Wavee.Spotify;

public class SpotifyConfig
{
    public SpotifyConfig(SpotifyRemoteConfig Remote,
        SpotifyPlaybackConfig Playback,
        SpotifyCacheConfig Cache, string locale)
    {
        this.Remote = Remote;
        this.Playback = Playback;
        this.Cache = Cache;
        Locale = locale;
    }

    public SpotifyRemoteConfig Remote { get; init; }
    public SpotifyPlaybackConfig Playback { get; init; }
    public SpotifyCacheConfig Cache { get; init; }
    public string Locale { get; set; }

    public void Deconstruct(out SpotifyRemoteConfig Remote, out SpotifyPlaybackConfig Playback, out SpotifyCacheConfig Cache)
    {
        Remote = this.Remote;
        Playback = this.Playback;
        Cache = this.Cache;
    }
}

/// <summary>
/// A config for the Spotify cache.
/// Wavee may use the cache to store audio files for faster playback.
/// </summary>
public class SpotifyCacheConfig
{
    /// <summary>
    /// A config for the Spotify cache.
    /// Wavee may use the cache to store audio files for faster playback.
    /// </summary>
    /// <param name="CachePath">
    /// The root directory of the cache.
    /// Note: The application must have write access to this directory.
    /// </param>
    /// <param name="CacheNoTouchExpiration">
    /// A sliding expiration for the cache.
    /// If a file has not been touched in this time, it will be deleted.
    /// Note: This uses the systems last access time, which may be skewed by other applications on the system.
    /// This defaults to 1 day.
    ///
    /// Metadata cache is not affected by this expiration, and is determined by the spoitfy response headers.
    /// </param>
    public SpotifyCacheConfig(Option<string> CachePath,
        Option<string> AudioCachePath,
        Option<TimeSpan> CacheNoTouchExpiration)
    {
        this.CachePath = CachePath;
        this.AudioCachePath = AudioCachePath;
        this.CacheNoTouchExpiration = CacheNoTouchExpiration;
    }

    /// <summary>
    /// The root directory of the cache.
    /// Note: The application must have write access to this directory.
    /// </summary>
    public Option<string> CachePath { get; set; }

    public Option<string> AudioCachePath { get; set; }

    /// <summary>
    /// A sliding expiration for the cache.
    /// If a file has not been touched in this time, it will be deleted.
    /// Note: This uses the systems last access time, which may be skewed by other applications on the system.
    /// This defaults to 1 day.
    /// Metadata cache is not affected by this expiration, and is determined by the spoitfy response headers.
    /// </summary>
    public Option<TimeSpan> CacheNoTouchExpiration { get; init; }

    public void Deconstruct(out Option<string> CachePath, out Option<string> AudioCachePath, out Option<TimeSpan> CacheNoTouchExpiration)
    {
        CachePath = this.CachePath;
        AudioCachePath = this.AudioCachePath;
        CacheNoTouchExpiration = this.CacheNoTouchExpiration;
    }
}

public class SpotifyPlaybackConfig
{
    public SpotifyPlaybackConfig(PreferredQualityType PreferredQualityType,
        Option<TimeSpan> CrossfadeDuration,
        bool Autoplay)
    {
        this.PreferredQualityType = PreferredQualityType;
        this.CrossfadeDuration = CrossfadeDuration;
        this.Autoplay = Autoplay;
    }

    public PreferredQualityType PreferredQualityType { get; set; }
    public Option<TimeSpan> CrossfadeDuration { get; set; }
    public bool Autoplay { get; set; }

    public void Deconstruct(out PreferredQualityType PreferredQualityType, out Option<TimeSpan> CrossfadeDuration, out bool Autoplay)
    {
        PreferredQualityType = this.PreferredQualityType;
        CrossfadeDuration = this.CrossfadeDuration;
        Autoplay = this.Autoplay;
    }
}

public record SpotifyRemoteConfig(string DeviceName,
    DeviceType DeviceType)
{
    public string DeviceName { get; set; } = DeviceName;
    public DeviceType DeviceType { get; set; } = DeviceType;

    public void Deconstruct(out string DeviceName, out DeviceType DeviceType)
    {
        DeviceName = this.DeviceName;
        DeviceType = this.DeviceType;
    }
}