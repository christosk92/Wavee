namespace Wavee.Spotify.Core.Clients.Remote;

public sealed class SpotifyRemoteRequestRequest
{
    public required string Key { get; init; }
    public required ReadOnlyMemory<byte> Data { get; init; }
}