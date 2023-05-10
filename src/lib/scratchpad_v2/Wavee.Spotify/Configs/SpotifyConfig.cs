using Eum.Spotify.connectstate;

namespace Wavee.Spotify.Configs;

public readonly record struct SpotifyConfig(
    SpotifyRemoteConfig Remote,
    SpotifyPlaybackConfig Playback
);

public readonly record struct SpotifyPlaybackConfig(PreferredQualityType PreferredQualityType);

public readonly record struct SpotifyRemoteConfig(
    string DeviceName,
    DeviceType DeviceType
);