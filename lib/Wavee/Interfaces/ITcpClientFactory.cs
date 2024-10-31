namespace Wavee.Interfaces;

internal interface ITcpClientFactory
{
    Task<ITcpClient> CreateAsync(string host, int port, CancellationToken cancellationToken);
}