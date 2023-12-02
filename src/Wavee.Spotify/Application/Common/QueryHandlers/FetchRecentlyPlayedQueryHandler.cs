using System.Text.Json;
using Mediator;
using Wavee.Spotify.Application.Common.Queries;
using Wavee.Spotify.Domain.Common;

namespace Wavee.Spotify.Application.Common.QueryHandlers;

public sealed class FetchRecentlyPlayedQueryHandler : IQueryHandler<FetchRecentlyPlayedQuery, IReadOnlyCollection<SpotifyRecentlyPlayedItem>>
{
    private readonly HttpClient _httpClient;

    public FetchRecentlyPlayedQueryHandler(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(Constants.SpotifyRemoteStateHttpClietn);
    }

    public async ValueTask<IReadOnlyCollection<SpotifyRecentlyPlayedItem>> Handle(FetchRecentlyPlayedQuery query, CancellationToken cancellationToken)
    {
        const string url =
            "https://spclient.com/recently-played/v3/user/{0}/recently-played?limit={1}&filter={2}&format=json";
        var finalUrl = string.Format(url, query.User, query.Limit, query.Filter);

        await using var response = await _httpClient.GetStreamAsync(finalUrl, cancellationToken);
        using var jsonDoc = await JsonDocument.ParseAsync(response, cancellationToken: cancellationToken);
        var contexts = jsonDoc.RootElement.GetProperty("playContexts");
        var output = new SpotifyRecentlyPlayedItem[contexts.GetArrayLength()];
        int i = 0;
        using var enumerator = contexts.EnumerateArray();
        while (enumerator.MoveNext())
        {
            var curr = enumerator.Current;
            var uri = curr.GetProperty("uri").GetString();
            var lastPlayedSec = curr.GetProperty("lastPlayedTime").GetInt64();
            var subItem = curr.GetProperty("lastPlayedTrackUri").GetString();

            output[i++] = new SpotifyRecentlyPlayedItem
            {
                Uri = uri,
                PlayedAt = DateTimeOffset.FromUnixTimeMilliseconds(lastPlayedSec),
                PlayedSubItem = subItem
            };
        }

        return output;
    }
}