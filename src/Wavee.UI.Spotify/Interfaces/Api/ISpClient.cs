using System.Threading;
using System.Threading.Tasks;
using Eum.Spotify.connectstate;
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
}