using Wavee.UI.Core.Contracts.Common;

namespace Wavee.UI.Core.Contracts.Home;

public interface IHomeView
{
    Task<IReadOnlyList<HomeGroup>> GetHomeViewAsync(string type, int limit, int contentLimit, CancellationToken none);
}