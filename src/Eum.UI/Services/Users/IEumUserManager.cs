using System.Collections;
using Eum.UI.Items;
using Eum.UI.Users;

namespace Eum.UI.Services.Users;

public interface IEumUserManager
{
    ValueTask<IEnumerable<EumUser>> GetUsers(bool refreshList);

    ValueTask<EumUser> AddUser(string profileName,
        string id,
        string? profilePicture,
        ServiceType serviceType,
        Dictionary<string, object> metadata);

    event EventHandler<EumUser> UserAdded;
    event EventHandler<EumUser> UserRemoved;
    event EventHandler<EumUser>? UserUpdated;
    EumUser GetUser(ItemId user);
}