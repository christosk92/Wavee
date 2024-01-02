namespace Wavee.Spotify.Core.Clients.Remote;

internal sealed class SpotifyRemoteRequestResult
{
    public required bool Success { get; init; }
    public required uint MessageId { get; init; }
    public required string SentByDeviceId { get; init; }
}

internal sealed class SpotifyRemoteMessage
{
}