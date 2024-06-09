using Wavee.Contracts.Interfaces.Contracts;

namespace Wavee.Contracts.Interfaces.Clients;

public interface IEpisodeClient
{
    Task<IEpisode> GetEpisode(IItemId episodeId, CancellationToken cancellationToken);
}