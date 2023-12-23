using Wavee.Spotify.Core.Interfaces.Connection;

namespace Wavee.Spotify.Infrastructure.Connection;

internal sealed class LiveSpotifyTcpClientFactory : ISpotifyTcpClientFactory 
{
    public ISpotifyTcpClient Create()
    {
        return new LiveSpotifyTcpClient();
    }
}