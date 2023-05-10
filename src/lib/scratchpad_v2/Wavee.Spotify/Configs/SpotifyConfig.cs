using Eum.Spotify.connectstate;

namespace Wavee.Spotify.Configs;

public readonly record struct SpotifyConfig(
    SpotifyRemoteConfig Remote
);

public readonly record struct SpotifyRemoteConfig(
    string DeviceName,
    DeviceType DeviceType
);