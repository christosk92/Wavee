using Eum.Spotify.connectstate;

namespace Wavee.Spotify.Infrastructure;

public readonly record struct SpotifyConfig(
    Option<string> CachePath,
    SpotifyRemoteConfig Remote,
    SpotifyPlaybackConfig Playback
);

public readonly record struct SpotifyPlaybackConfig(
    PreferredQualityType PreferredQualityType,
    bool Autoplay
);

public readonly record struct SpotifyRemoteConfig(
    string DeviceName,
    DeviceType DeviceType
);