using Eum.Spotify.connectstate;
using Wavee.Spotify.Core.Clients.Remote;
using Wavee.Spotify.Infrastructure.HttpClients;
using Wavee.Spotify.Infrastructure.WebSocket;
using Wavee.Spotify.Interfaces;
using Wavee.Spotify.Interfaces.WebSocket;

namespace Wavee.Spotify.Infrastructure.Services;

internal sealed class WebSocketService : IWebSocketService
{
    private ActiveWebSocket? _activeWebSocket;

    private readonly ISpotifyWebSocketFactory _socketFactory;
    private readonly ISpotifyTokenService _tokenService;
    private readonly IApResolverService _apResolverService;
    private readonly WaveeSpotifyConfig _config;
    private readonly SpotifyInternalHttpClient _httpClient;
    
    private readonly SemaphoreSlim _reconnectSemaphore;
    private readonly SemaphoreSlim _connectionSemaphore;

    private bool _dontReconnect;

    private string? _activeConnectionId;
    public WebSocketService(ISpotifyTokenService tokenService,
        ISpotifyWebSocketFactory socketFactory,
        IApResolverService apResolverService,
        WaveeSpotifyConfig config, 
        SpotifyInternalHttpClient httpClient)
    {
        _tokenService = tokenService;
        _socketFactory = socketFactory;
        _apResolverService = apResolverService;
        this._config = config;
        _httpClient = httpClient;

        _reconnectSemaphore = new SemaphoreSlim(1, 1);
        _connectionSemaphore = new SemaphoreSlim(1, 1);
    }

    public async ValueTask<string> ConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _connectionSemaphore.WaitAsync(cancellationToken);
            if (_activeWebSocket is { Connected: true } && _activeConnectionId is not null)
            {
                return _activeConnectionId;
            }
            _activeWebSocket?.Dispose();
            _activeConnectionId = null;
            
            var (host, port) = await _apResolverService.GetDealer(cancellationToken);
            _activeWebSocket = new ActiveWebSocket(_socketFactory, _tokenService, _config, _httpClient, OnRequest);
            _activeWebSocket.OnError += ConnectionOnOnError;
            _activeWebSocket.OnClusterUpdate += OnClusterUpdate;

            _activeConnectionId = await _activeWebSocket.ConnectAsync(host, port, cancellationToken);

            
            return _activeConnectionId!;
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    private Task<SpotifyRemoteRequestResult> OnRequest(SpotifyRemoteRequestRequest arg)
    {
        return Task.FromResult(new SpotifyRemoteRequestResult
        {
            Success = true
        });
    }

    private void OnClusterUpdate(object? sender, ClusterUpdate e)
    {
        
    }


    private void ConnectionOnOnError(object? sender, Exception e)
    {
        throw new NotImplementedException();
    }
}