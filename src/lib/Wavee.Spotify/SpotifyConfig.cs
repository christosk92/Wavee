using Eum.Spotify.connectstate;
using LanguageExt;
using Wavee.Spotify.Infrastructure.Playback;

namespace Wavee.Spotify;

public record SpotifyConfig(
    Option<string> CachePath,
    SpotifyRemoteConfig Remote,
    SpotifyPlaybackConfig Playback
);

public record SpotifyPlaybackConfig(
    PreferredQualityType PreferredQualityType,
    Option<TimeSpan> CrossfadeDuration,
    bool Autoplay);

public readonly record struct SpotifyRemoteConfig(
    string DeviceName,
    DeviceType DeviceType
);