using Wavee.UI.Spotify.Interfaces;

namespace Wavee.UI.Spotify.Playback;

internal sealed class SpotifyWebsocketConnectionFactory : ISpotifyWebsocketConnectionFactory
{
    public ISpotifyWebsocketConnection Create(string url, ISpotifyMessageHandler messageHandler, ISpotifyRequestHandler requestHandler)
    {
        return new SpotifyWebsocketConnection(url,messageHandler, requestHandler);
    }
}