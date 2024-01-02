using Wavee.Spotify.Interfaces.Connection;

namespace Wavee.Spotify.Infrastructure.Connection;

internal sealed class LiveSpotifyTcpClientFactory : ISpotifyTcpClientFactory 
{
    public ISpotifyTcpClient Create()
    {
        return new LiveSpotifyTcpClient();
    }
}