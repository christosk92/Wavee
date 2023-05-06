using Eum.Spotify.connectstate;

namespace Wavee.Spotify;

public readonly record struct SpotifyConfig(
    string DeviceName,
    DeviceType DeviceType);