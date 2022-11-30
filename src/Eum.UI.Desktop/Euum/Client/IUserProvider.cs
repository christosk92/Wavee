using Eum.UI.Users;

namespace Eum.UI.Euum.Client;

public interface IUserProvider
{
    Task<IEnumerable<IEumUser>> GetUsersAsync();
}
