using Eum.Spotify;
using Microsoft.Extensions.Logging;
using Wavee.Interfaces;
using OAuthAuthenticator = Wavee.Services.Session.OAuthAuthenticator;

namespace Wavee.Services;

internal sealed class OAuthClient : IOAuthClient
{
    private readonly ILogger<OAuthClient> _logger;
    private readonly HttpClient _httpClient;

    public OAuthClient(HttpClient httpClient, ILogger<OAuthClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<LoginCredentials?> LoginAsync(string clientId, string scopes, CancellationToken cancellationToken)
    {
        var result = await OAuthAuthenticator.AuthenticateAsync(_httpClient, clientId, scopes, cancellationToken);
        return result;
    }
}