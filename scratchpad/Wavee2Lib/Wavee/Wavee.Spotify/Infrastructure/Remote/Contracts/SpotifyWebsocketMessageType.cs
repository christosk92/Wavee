namespace Wavee.Spotify.Infrastructure.Remote.Contracts;

public enum SpotifyWebsocketMessageType
{
    ConnectionId,
    Message,
    Request,
    Pong
}