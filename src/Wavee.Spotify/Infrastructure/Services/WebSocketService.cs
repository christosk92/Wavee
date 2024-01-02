using System.Text.Json;
using Eum.Spotify.connectstate;
using Eum.Spotify.transfer;
using Wavee.Core.Playback;
using Wavee.Spotify.Core.Clients.Remote;
using Wavee.Spotify.Core.Playback;
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

    public event EventHandler<SpotifyContextBuilder.FinalizedBuilder> PlayRequested;
    public event EventHandler<TimeSpan> SeekRequested; 
    
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

    public async Task PutState(PutStateRequest request, CancellationToken cancellationToken)
    {
        if (_activeConnectionId is null)
        {
            await ConnectAsync(cancellationToken);
        }

        await _httpClient.PutStateAsync(_activeConnectionId!, request, cancellationToken);
    }

    public Cluster? LatestCluster { get; private set; }
    public event EventHandler<ClusterUpdate>? ClusterChanged;

    private async Task<SpotifyRemoteRequestResult> OnRequest(SpotifyRemoteRequestRequest arg)
    {
        using var jsondoc = JsonDocument.Parse(arg.Data);
        var x = jsondoc.RootElement;
        var messageId = x.GetProperty("message_id").GetUInt32();
        var sentByDeviceid = x.GetProperty("sent_by_device_id").GetString();
        var command = x.GetProperty("command");
        var endpoint = command.GetProperty("endpoint").GetString();
        switch (endpoint)
        {
            case "transfer":
            {
                ReadOnlyMemory<byte> payload = command.GetProperty("data").GetBytesFromBase64();
                var stateToTransfer = TransferState.Parser.ParseFrom(payload.Span);
                var context = SpotifyContextBuilder.New()
                    .FromTransferState(stateToTransfer);

                PlayRequested?.Invoke(this, context);
                break;
            }
            case "seek_to":
            {
                var value = command.GetProperty("value").GetDouble();
                var position = TimeSpan.FromMilliseconds(value);
                
                SeekRequested?.Invoke(this, position);
                break;
            }
        }


        return new SpotifyRemoteRequestResult
        {
            Success = true,
            MessageId = messageId,
            SentByDeviceId = sentByDeviceid!
        };
        //
        //
        // return Task.FromResult(new SpotifyRemoteRequestResult
        // {
        //     Success = true
        // });
    }

    private void OnClusterUpdate(object? sender, ClusterUpdate e)
    {
        LatestCluster = e.Cluster;
        ClusterChanged?.Invoke(this, e);
    }


    private void ConnectionOnOnError(object? sender, Exception e)
    {
        // TODO: Handle errors
        LatestCluster = null;
        ClusterChanged?.Invoke(this, new ClusterUpdate());
    }
}