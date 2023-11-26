using Wavee.Spotify.Application.Authentication.Modules;

namespace Wavee.Spotify.Common.Contracts;

public interface ISpotifyAuthModule
{
    Task<StoredCredentials> GetCredentials(CancellationToken cancellationToken = default);
}