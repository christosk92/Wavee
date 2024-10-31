using Eum.Spotify;

namespace Wavee.Interfaces;

internal interface ICredentialsProvider
{
    string ClientId { get; }
    string Scopes { get; }
    ValueTask<LoginCredentials> GetUserCredentialsAsync(CancellationToken cancellationToken);
}