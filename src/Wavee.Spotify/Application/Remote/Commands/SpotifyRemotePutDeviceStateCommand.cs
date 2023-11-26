using Eum.Spotify.connectstate;
using Mediator;

namespace Wavee.Spotify.Application.Remote.Commands;

public sealed class SpotifyRemotePutDeviceStateCommand : ICommand<Cluster>
{
    public required PutStateRequest State { get; init; }
    public required string ConnectionId { get; init; }
}