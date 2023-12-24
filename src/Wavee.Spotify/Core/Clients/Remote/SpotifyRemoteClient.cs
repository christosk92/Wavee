using Wavee.Spotify.Interfaces;
using Wavee.Spotify.Interfaces.Clients;

namespace Wavee.Spotify.Core.Clients.Remote;

internal sealed class SpotifyRemoteClient : ISpotifyRemoteClient
{
    private string? _activeConnectionId;
    private readonly IWebSocketService _webSocketService;
    public SpotifyRemoteClient(IWebSocketService webSocketService)
    {
        _webSocketService = webSocketService;
    }

    public async ValueTask<bool> Connect(CancellationToken cancellationToken = default)
    {
        var connectionId = await _webSocketService.ConnectAsync(cancellationToken: cancellationToken);
        if (_activeConnectionId != connectionId)
        {
            _activeConnectionId = connectionId;
            return true;
        }
        
        return false;
    }
}