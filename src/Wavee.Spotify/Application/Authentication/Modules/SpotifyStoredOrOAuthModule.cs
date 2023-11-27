using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Wavee.Spotify.Common.Contracts;
using Wavee.Spotify.Infrastructure.LegacyAuth;
using Wavee.Spotify.Infrastructure.Persistent;

namespace Wavee.Spotify.Application.Authentication.Modules;

public sealed class SpotifyStoredOrOAuthModule : ISpotifyAuthModule
{
    private readonly Func<SpotifyOAuthModule> _oAuthModuleFactory;
    private readonly Func<SpotifyStoredCredentialsModule> _storedCredentialsModuleFactory;

    public SpotifyStoredOrOAuthModule(
        FetchRedirectUrlDelegate fetchRedirectUrl,
        IServiceProvider provider)
    {
        _oAuthModuleFactory = () =>
        {
            return new SpotifyOAuthModule(
                fetchRedirectUrl,
                httpClientFactory: provider.GetRequiredService<IHttpClientFactory>(),
                provider.GetRequiredService<SpotifyClientConfig>(),
                provider.GetRequiredService<SpotifyTcpHolder>()
            );
        };
        _storedCredentialsModuleFactory = () =>
        {
            return new SpotifyStoredCredentialsModule(
                provider.GetRequiredService<IMediator>(),
                spotifyStoredCredentialsRepository: provider.GetRequiredService<ISpotifyStoredCredentialsRepository>(),
                provider.GetRequiredService<SpotifyTcpHolder>(),
                provider.GetRequiredService<SpotifyClientConfig>()
            );
        };
    }

    public bool IsDefault { get; private set; }

    public async Task<StoredCredentials> GetCredentials(string? username, CancellationToken cancellationToken = default)
    {
        var storedCredentialsModule = _storedCredentialsModuleFactory();
        try
        {
            var stored = await storedCredentialsModule.GetCredentials(username, cancellationToken);
            IsDefault = storedCredentialsModule.IsDefault;
            return stored;
        }
        catch (Exception)
        {
            var oauthResponse = await _oAuthModuleFactory().GetCredentials(username, cancellationToken);
            IsDefault = oauthResponse.IsDefault;
            return oauthResponse;
        }
    }
}