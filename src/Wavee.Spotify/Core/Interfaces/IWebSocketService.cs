namespace Wavee.Spotify.Core.Interfaces;

internal interface IWebSocketService
{
    ValueTask<string> ConnectAsync(CancellationToken cancellationToken);
}