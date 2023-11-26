namespace Wavee.Spotify;

public sealed class SpotifyClientConfig
{
    public required StorageSettings Storage { get; init; }
    public required SpotifyRemoteConfig Remote { get; init; }
}

public sealed class SpotifyRemoteConfig
{
    public string DeviceId { get; } = Guid.NewGuid().ToString();
}

public sealed class StorageSettings
{
    public required string Path { get; init; }
}