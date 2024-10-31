using Eum.Spotify;
using Wavee.Services.Session;

namespace Wavee.Interfaces;

internal interface ITcpClient : IDisposable
{
    bool Connected { get; }
    ValueTask ConnectAsync(string host, int port, CancellationToken cancellationToken);

    Task<LoginCredentials> Initialize(LoginCredentials storedCredentials, string deviceId,
        CancellationToken cancellationToken);

    Stream GetStream();
    Task<SpotifyTcpMessage> ReadMessageAsync(CancellationToken token);
    Task SendPacketAsync(SpotifyTcpMessage message, CancellationToken cancellationToken);
}