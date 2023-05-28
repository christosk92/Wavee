using Eum.Spotify.connectstate;
using LanguageExt;
using Wavee.Spotify.Infrastructure.Playback;

namespace Wavee.Spotify;

public class SpotifyConfig
{
    public SpotifyConfig(SpotifyRemoteConfig Remote,
        SpotifyPlaybackConfig Playback,
        SpotifyCacheConfig Cache)
    {
        this.Remote = Remote;
        this.Playback = Playback;
        this.Cache = Cache;
    }

    public SpotifyRemoteConfig Remote { get; init; }
    public SpotifyPlaybackConfig Playback { get; init; }
    public SpotifyCacheConfig Cache { get; init; }

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
public record SpotifyCacheConfig(
    Option<string> CachePath,
    Option<TimeSpan> CacheNoTouchExpiration);

public record SpotifyPlaybackConfig(
    PreferredQualityType PreferredQualityType,
    Option<TimeSpan> CrossfadeDuration,
    bool Autoplay);

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