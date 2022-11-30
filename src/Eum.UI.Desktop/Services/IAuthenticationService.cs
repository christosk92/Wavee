using Eum.UI.Users;

namespace Eum.UI.Services;

public interface IAuthenticationService : ISupportedService
{
    Task<PartialUser> AuthenticateUsernamePassword(string username, string password, CancellationToken ct = default);
}