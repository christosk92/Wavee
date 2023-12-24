using Eum.Spotify;

namespace Wavee.Spotify.Interfaces;

internal interface IAuthenticationService
{
    Task<(LoginCredentials credentials, string deviceId)> GetCredentials(CancellationToken cancellationToken = default);
}