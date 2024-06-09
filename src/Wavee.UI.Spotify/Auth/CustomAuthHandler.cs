using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Wavee.UI.Spotify.Clients;
using Wavee.UI.Spotify.Exceptions;

namespace Wavee.UI.Spotify.Auth;

internal sealed class CustomAuthHandler(SpotifyTokenClient tokensClient) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Send the request
        var token = await tokensClient.GetToken(cancellationToken);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (content is "Token expired")
            {
                throw new SpotifyException(SpotifyFailureReason.TokenExpired);
            }
        }

        return response;
    }
}