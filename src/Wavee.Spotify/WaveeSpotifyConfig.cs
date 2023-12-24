using Eum.Spotify.connectstate;
using Wavee.Interfaces;
using Wavee.Spotify.Interfaces;

namespace Wavee.Spotify;

public sealed class WaveeSpotifyConfig
{
    public WaveeSpotifyRemoteConfig Remote { get; } = new();
    public WaveeSpotifyPlaybackConfig Playback { get; } = new();

    public required WaveeSpotifyCredentialsStorageConfig CredentialsStorage { get; init; }
    public required IWaveeCachingProvider? CachingProvider { get; init; } = null;
}

public sealed class WaveeSpotifyCredentialsStorageConfig
{
    public required Func<string?> GetDefaultUsername { get; init; } = () => throw new NotImplementedException();

    public required Func<string, SpotifyCredentialsType, byte[]?> OpenCredentials { get; init; } =
        (_, _) => throw new NotImplementedException();

    public required Action<string, SpotifyCredentialsType, byte[]> SaveCredentials { get; init; } =
        (_, _, _) => throw new NotImplementedException();
}

public sealed class WaveeSpotifyPlaybackConfig
{
    public double InitialVolume { get; internal set; } = 0.5;
    public WaveeSpotifyPreferedQuality PreferedQuality { get; internal set; }  = WaveeSpotifyPreferedQuality.Normal;
}

public enum WaveeSpotifyPreferedQuality
{
    Low,
    Normal,
    High
}

public sealed class WaveeSpotifyRemoteConfig
{
    public string DeviceId { get; } = Guid.NewGuid().ToString();
    public string DeviceName { get; internal set; } = "Wavee";
    public DeviceType Type { get; internal set; } = DeviceType.Computer;
}