using Wavee.Contracts.Interfaces.Contracts;
using Wavee.Contracts.Models;

namespace Wavee.Contracts.Interfaces.Clients;

public interface IHomeClient
{
    Task<IReadOnlyCollection<IHomeItem>> GetItems(CancellationToken cancellation);
    Task<IReadOnlyCollection<IHomeItem>> GetRecentlyPlayed(HomeGroup group, CancellationToken cancellation);
}