using Wavee.Spotify.Interfaces.WebSocket;

namespace Wavee.Spotify.Infrastructure.WebSocket;

internal sealed class LiveSpotifyWebSocketFactory : ISpotifyWebSocketFactory
{
    public ISpotifyWebSocket Create()
    {
        return new LiveSpotifyWebSocket();
    }
}