namespace Wavee.Spotify.Infrastructure.HttpClients;

internal sealed class ApResolverHttpClient
{
    public ApResolverHttpClient(HttpClient client)
    {
        Client = client;
    }
    
    public HttpClient Client { get; }
}