using Microsoft.Extensions.Logging;
using Wavee.Interfaces;

namespace Wavee.Services.Session;

internal sealed class TcpClientFactory : ITcpClientFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public TcpClientFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public async Task<ITcpClient> CreateAsync(string host, int port, CancellationToken cancellationToken)
    {
        var tcpClient = new WaveeTcpClient(_loggerFactory.CreateLogger<WaveeTcpClient>());
        await tcpClient.ConnectAsync(host, port, cancellationToken);
        return tcpClient;
    }
}