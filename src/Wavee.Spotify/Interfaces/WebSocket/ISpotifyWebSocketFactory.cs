namespace Wavee.Spotify.Interfaces.WebSocket;

internal interface ISpotifyWebSocketFactory
{
    ISpotifyWebSocket Create();
}