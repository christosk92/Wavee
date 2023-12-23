using Eum.Spotify;

namespace Wavee.Spotify.Core.Interfaces;

internal interface IAuthenticationService
{
    Task<(LoginCredentials credentials, string deviceId)> GetCredentials(CancellationToken cancellationToken = default);
}