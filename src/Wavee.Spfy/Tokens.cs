using Eum.Spotify;

namespace Wavee.Spfy;

internal static class Tokens
{
    public static async Task<SpotifyTokenResult> GetToken(Guid instanceId, IHttpClient servicesHttpClient,
        string deviceId)
    {
        if (!EntityManager.TryGetConnection(instanceId, out var _, out var welcomeMessage))
        {
            //TODO: Should we reconnect here, wait here... Like whats the plan!!!!!!
            throw new InvalidOperationException("No connection found");
        }

        var response = await servicesHttpClient.SendLoginStepTwoRequest(new LoginCredentials
        {
            AuthData = welcomeMessage.ReusableAuthCredentials,
            Username = welcomeMessage.CanonicalUsername,
            Typ = welcomeMessage.ReusableAuthCredentialsType
        }, deviceId, CancellationToken.None);

        return new SpotifyTokenResult(
            accessToken: response.Ok.AccessToken,
            addSeconds: DateTimeOffset.UtcNow.AddSeconds(response.Ok.AccessTokenExpiresIn),
            finalUsername: response.Ok.Username
        );
    }
}