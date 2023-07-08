using System.Web;
using Wavee.UI.Client.Search;

namespace Wavee.UI.Spotify.Clients;

internal sealed class SpotifyUISearchClient : IWaveeUISearchClient
{
    private readonly WeakReference<SpotifyClient> _spotifyClient;

    public SpotifyUISearchClient(SpotifyClient spotifyClient)
    {
        _spotifyClient = new System.WeakReference<SpotifyClient>(spotifyClient);
    }

    public async Task<ReadOnlyMemory<byte>> GetSearchResultsAsync(string query, CancellationToken cancellationToken = default)
    {
        const string url =
            "hm://searchview/km/v4/search/{0}?entityVersion=4&limit={1}&imageSize=large&catalogue=&country={2}&platform=zelda&username={3}";
      
        if(!_spotifyClient.TryGetTarget(out var client))
            throw new InvalidOperationException("Spotify client is not available");
        var username = client.WelcomeMessage.CanonicalUsername;
        var country = await client.Country;
        var url2 = string.Format(url, HttpUtility.UrlEncode(query), 10, country, username);

        var response = await client.Mercury.Get(url2, cancellationToken);
        return response.Payload;
    }
}