namespace Wavee.Contracts.Interfaces.Clients;

public interface ITrackClient
{
    Task<ITrack> GetTrack(IItemId id, CancellationToken cancellationToken = default);
}