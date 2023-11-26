using Mediator;
using Wavee.Spotify.Application.Common.Queries;

namespace Wavee.Spotify.Application.Common.QueryHandlers;

public sealed class SpotifyGetAdaptiveApiUrlQueryHandler : IQueryHandler<SpotifyGetAdaptiveApiUrlQuery, SpotifyGetAdaptiveApiUrl>
{
    private readonly HttpClient _httpClient;

    public SpotifyGetAdaptiveApiUrlQueryHandler(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(Constants.SpotifyAccountsApiHttpClient);
    }

    public ValueTask<SpotifyGetAdaptiveApiUrl> Handle(SpotifyGetAdaptiveApiUrlQuery query, CancellationToken cancellationToken)
    {
        //TODO: Implement
        switch (query.Type)
        {
            case SpotifyApiUrlType.AccessPoint:
                return new ValueTask<SpotifyGetAdaptiveApiUrl>(
                    new SpotifyGetAdaptiveApiUrl("ap-gae2.spotify.com", 4070));
                break;
            case SpotifyApiUrlType.Dealer:
                return new ValueTask<SpotifyGetAdaptiveApiUrl>(
                    new SpotifyGetAdaptiveApiUrl("gae2-dealer.spotify.com", 4070));
                break;
            case SpotifyApiUrlType.SpClient:
                return new ValueTask<SpotifyGetAdaptiveApiUrl>(
                    new SpotifyGetAdaptiveApiUrl("gae2-spclient.spotify.com", 4070));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    /*
     * {
    "accesspoint": [
        "ap-gae2.spotify.com:4070",
        "ap-gae2.spotify.com:443",
        "ap-gae2.spotify.com:80",
        "ap-gew1.spotify.com:4070",
        "ap-guc3.spotify.com:443",
        "ap-gue1.spotify.com:80"
    ],
    "dealer": [
        "gae2-dealer.spotify.com:443",
        "gew1-dealer.spotify.com:443",
        "guc3-dealer.spotify.com:443",
        "gue1-dealer.spotify.com:443"
    ],
    "spclient": [
        "gae2-spclient.spotify.com:443",
        "gew1-spclient.spotify.com:443",
        "guc3-spclient.spotify.com:443",
        "gue1-spclient.spotify.com:443"
    ]
}
     */
}