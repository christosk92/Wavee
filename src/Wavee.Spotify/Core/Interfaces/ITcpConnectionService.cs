using Eum.Spotify;

namespace Wavee.Spotify.Core.Interfaces;

internal interface ITcpConnectionService : IDisposable
{
    Task<APWelcome> ConnectAsync(CancellationToken cancellationToken);
    
    APWelcome? WelcomeMessage { get; }
}