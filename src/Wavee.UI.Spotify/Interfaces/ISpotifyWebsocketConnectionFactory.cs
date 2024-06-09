using Wavee.UI.Spotify.Playback;

namespace Wavee.UI.Spotify.Interfaces;

internal interface ISpotifyWebsocketConnectionFactory
{
    ISpotifyWebsocketConnection Create(string url, ISpotifyMessageHandler messageHandler,
        ISpotifyRequestHandler requestHandler);
}