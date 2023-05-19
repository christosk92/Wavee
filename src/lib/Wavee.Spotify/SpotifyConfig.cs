using Eum.Spotify.connectstate;
using LanguageExt;
using Wavee.Spotify.Infrastructure.Playback;

namespace Wavee.Spotify;

public record SpotifyConfig(
    SpotifyRemoteConfig Remote,
    SpotifyPlaybackConfig Playback,
    SpotifyCacheConfig Cache
);

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

public readonly record struct SpotifyRemoteConfig(
    string DeviceName,
    DeviceType DeviceType
);