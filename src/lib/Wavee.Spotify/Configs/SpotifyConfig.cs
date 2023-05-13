using Eum.Spotify.connectstate;
using Wavee.Core.Enums;
using Wavee.Spotify.Playback;

namespace Wavee.Spotify.Configs;

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