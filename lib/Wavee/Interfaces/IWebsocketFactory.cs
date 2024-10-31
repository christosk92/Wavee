namespace Wavee.Interfaces;

internal interface IWebsocketFactory
{
    Task<ISpotifyWebsocket> CreateWebsocket(CancellationToken token);
}