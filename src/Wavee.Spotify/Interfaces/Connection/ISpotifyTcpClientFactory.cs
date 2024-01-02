namespace Wavee.Spotify.Interfaces.Connection;

internal interface ISpotifyTcpClientFactory 
{
    ISpotifyTcpClient Create();
}