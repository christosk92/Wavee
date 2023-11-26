using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Wavee.Spotify.Common.Contracts;

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
                provider.GetRequiredService<IMediator>(),
                provider.GetRequiredService<SpotifyClientConfig>()
            );
        };
        _storedCredentialsModuleFactory = () =>
        {
            return new SpotifyStoredCredentialsModule(
                config: provider.GetRequiredService<SpotifyClientConfig>(),
                provider.GetRequiredService<IMediator>()
            );
        };
    }

    public async Task<StoredCredentials> GetCredentials(CancellationToken cancellationToken = default)
    {
        var storedCredentialsModule = _storedCredentialsModuleFactory();
        try
        {
            var stored = await storedCredentialsModule.GetCredentials(cancellationToken);
            return stored;
        }
        catch (Exception)
        {
            var oauthResponse = await _oAuthModuleFactory().GetCredentials(cancellationToken);
            return oauthResponse;
        }
    }
}