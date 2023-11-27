using Eum.Spotify.connectstate;

namespace Wavee.Spotify;

public sealed class SpotifyClientConfig
{
    public required StorageSettings Storage { get; init; }
    public required SpotifyRemoteConfig Remote { get; init; }
    public required SpotifyPlaybackConfig Playback { get; init; }
}

public sealed class SpotifyPlaybackConfig
{
    public required double InitialVolume { get; init; }
    public required SpotifyAudioQuality PreferedQuality { get; init; }
}

public enum SpotifyAudioQuality
{
    Normal,
    High,
    VeryHigh
}

public sealed class SpotifyRemoteConfig
{
    public string DeviceId { get; } = Guid.NewGuid().ToString();
    public required string DeviceName { get; init; }
    public required DeviceType DeviceType { get; init; }
}

public sealed class StorageSettings
{
    public required string Path { get; init; }
}