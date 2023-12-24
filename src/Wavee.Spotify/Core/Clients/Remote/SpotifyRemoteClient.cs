using Wavee.Interfaces;
using Wavee.Spotify.Interfaces;
using Wavee.Spotify.Interfaces.Clients;

namespace Wavee.Spotify.Core.Clients.Remote;

internal sealed class SpotifyRemoteClient : ISpotifyRemoteClient
{
    private string? _activeConnectionId;
    private readonly IWebSocketService _webSocketService;
    private readonly IWaveePlayer _player;
    public SpotifyRemoteClient(IWebSocketService webSocketService, IWaveePlayer player)
    {
        _webSocketService = webSocketService;
        _player = player;
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