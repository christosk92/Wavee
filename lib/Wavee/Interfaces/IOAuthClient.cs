using Eum.Spotify;

namespace Wavee.Interfaces;

public interface IOAuthClient
{
    Task<LoginCredentials?> LoginAsync(string clientId, string scopes, CancellationToken cancellationToken);
}