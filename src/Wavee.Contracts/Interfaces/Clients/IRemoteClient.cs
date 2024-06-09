using Eum.Spotify.connectstate;
using Wavee.Contracts.Interfaces.Contracts;

namespace Wavee.Contracts.Interfaces.Clients;

public interface IPlaybackClient
{
    Task<IPlaybackDevice> Connect(string name, DeviceType type, CancellationToken cancellationToken);
}