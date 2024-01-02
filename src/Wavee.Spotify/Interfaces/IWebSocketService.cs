using Eum.Spotify.connectstate;

namespace Wavee.Spotify.Interfaces;

internal interface IWebSocketService
{
    ValueTask<string> ConnectAsync(CancellationToken cancellationToken);

    Task PutState(PutStateRequest request, CancellationToken cancellationToken);
    Cluster? LatestCluster { get; }
    event EventHandler<ClusterUpdate> ClusterChanged;
}