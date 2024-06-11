using System.Threading;
using System.Threading.Tasks;
using Eum.Spotify.connectstate;
using Eum.Spotify.storage;
using Refit;
using Spotify.Metadata;
using Wavee.Contracts.Interfaces;

namespace Wavee.UI.Spotify.Interfaces.Api;

public interface ISpClient
{
    [Put("/connect-state/v1/devices/{deviceId}")]
    Task<Cluster> PutState(
        [Body(BodySerializationMethod.Serialized)] PutStateRequest request,
        string deviceId,
        [Header("X-Spotify-Connection-Id")] string connectionId,
        CancellationToken cancellationToken = default);

    [Get("/metadata/4/track/{trackId}?market=from_token")]
    Task<Track> GetTrack(string trackId, CancellationToken cancellationToken);

    [Get("/storage-resolve/files/audio/interactive_prefetch/{fileId}")]
    Task<StorageResolveResponse> InteractivePrefetch(string fileId, CancellationToken cancellationToken);
    
    
    [Get("/storage-resolve/files/audio/interactive/{fileId}")]
    Task<StorageResolveResponse> Interactive(string fileId, CancellationToken cancellationToken);
}