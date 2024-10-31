namespace Wavee.Services.Playback.Remote;

internal sealed class SpotifyWebsocketMessage
{
    public required Guid MessageId { get; init; }
    public required SpotifyWebsocketMessageType Type { get; init; }
    public required string Uri { get; set; }
    public required byte[] Payload { get; set; }
}

internal enum SpotifyWebsocketMessageType
{
    Message,
    Request
}