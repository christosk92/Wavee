using System.Net.Http.Headers;
using Wavee.Interfaces;

namespace Wavee.HttpHandlers;

internal sealed class AuthenticatedHttpClientHandler : DelegatingHandler
{
    private readonly ISpotifyTokenClient _tokenClient;

    public AuthenticatedHttpClientHandler(ISpotifyTokenClient tokenClient)
    {
        _tokenClient = tokenClient;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await _tokenClient.GetBearerToken(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
        var clientToken = await _tokenClient.GetClientToken(cancellationToken);
        request.Headers.Add("client-token", clientToken.Value);
        var result = await base.SendAsync(request, cancellationToken);
        return result;
    }
}