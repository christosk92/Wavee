namespace Wavee.Spotify.Core.Interfaces.WebSocket;

internal interface ISpotifyWebSocketFactory
{
    ISpotifyWebSocket Create();
}