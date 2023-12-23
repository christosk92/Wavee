using Eum.Spotify;
using Wavee.Spotify.Core.Interfaces;
using Wavee.Spotify.Core.Interfaces.Connection;
using Wavee.Spotify.Infrastructure.Connection;

namespace Wavee.Spotify.Infrastructure.Services;

internal sealed class TcpConnectionService : ITcpConnectionService
{
    private ActiveTcpConnection? _connection;

    private readonly ISpotifyTcpClientFactory _tcpClientFactory;
    private readonly IAuthenticationService _authenticationService;
    private readonly IApResolverService _apResolverService;
    private readonly SemaphoreSlim _reconnectSemaphore;
    private readonly SemaphoreSlim _connectionSemaphore;
    
    private bool _dontReconnect;

    public TcpConnectionService(
        IApResolverService apResolverService,
        IAuthenticationService authenticationService,
        ISpotifyTcpClientFactory tcpClientFactory)
    {
        _apResolverService = apResolverService;
        _authenticationService = authenticationService;
        _tcpClientFactory = tcpClientFactory;

        _reconnectSemaphore = new SemaphoreSlim(1, 1);
        _connectionSemaphore = new SemaphoreSlim(1, 1);
    }

    public async Task<APWelcome> ConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _connectionSemaphore.WaitAsync(cancellationToken);
            if (_connection?.ApWelcome is not null)
            {
                return await Task.FromResult(_connection.ApWelcome);
            }

            var (host, port) = await _apResolverService.GetAccessPoint(cancellationToken);
            _connection = new ActiveTcpConnection(_tcpClientFactory, _authenticationService);
            _connection.OnError += ConnectionOnOnError;
            
            return await _connection.ConnectAsync(host, port);
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    public APWelcome? WelcomeMessage => _connection?.ApWelcome;

    private async void ConnectionOnOnError(object? sender, Exception e)
    {
        var instanceBefore = _connection;
        await _reconnectSemaphore.WaitAsync();
        try
        {
            if (_dontReconnect || _connection is null || _connection != instanceBefore)
            {
                return;
            }
            
            _connection?.Dispose();
            _connection = null;
            await ConnectAsync(CancellationToken.None);
        }
        finally
        {
            _reconnectSemaphore.Release();
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _reconnectSemaphore.Dispose();
    }
}