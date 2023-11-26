using Eum.Spotify.connectstate;
using Mediator;
using Wavee.Spotify.Domain.State;

namespace Wavee.Spotify.Application.Remote.Queries;

public sealed class ClusterToPlaybackStateQuery : IRequest<SpotifyPlaybackState>
{
    public required Cluster Cluster { get; init; }
}