using Eum.Spotify.connectstate;
using Wavee.Spotify.Playback.Sys;

namespace Wavee.Spotify.Remote;

public readonly record struct SpotifyPlaybackConfig(string DeviceName, DeviceType DeviceType, float InitialVolume,
    PreferredQualityType PreferredQuality);