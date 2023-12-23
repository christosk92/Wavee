namespace Wavee.Spotify.Core.Interfaces.Connection;

internal interface ISpotifyTcpClientFactory 
{
    ISpotifyTcpClient Create();
}