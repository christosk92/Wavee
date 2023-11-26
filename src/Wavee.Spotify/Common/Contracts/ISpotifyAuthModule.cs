using Wavee.Spotify.Application.Authentication.Modules;

namespace Wavee.Spotify.Common.Contracts;

public interface ISpotifyAuthModule
{
    bool IsDefault { get; }
    Task<StoredCredentials> GetCredentials(string? username, CancellationToken cancellationToken = default);
}