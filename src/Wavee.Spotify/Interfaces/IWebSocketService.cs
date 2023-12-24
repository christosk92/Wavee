namespace Wavee.Spotify.Interfaces;

internal interface IWebSocketService
{
    ValueTask<string> ConnectAsync(CancellationToken cancellationToken);
}