using Mediator;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using LanguageExt.Pretty;
using Wavee.Spotify.Application.Search.Queries;
using Wavee.Spotify.Common;

namespace Wavee.Spotify.Application.Search;

internal sealed class SpotifySearchClient : ISpotifySearchClient
{
    private readonly IMediator _mediator;
    private readonly HttpClient _httpClient;
    public SpotifySearchClient(IMediator mediator, IHttpClientFactory httpClient)
    {
        _mediator = mediator;
        _httpClient = httpClient.CreateClient(Constants.SpotifyRemoteStateHttpClietn);
    }

    public async Task<SpotifySearchResult> SearchAsync(string query,
        int offset = 0,
        int limit = 10,
        int numberOfTopResults = 5,
        CancellationToken cancellationToken = default)
    {
        var cmd = new SpotifySearchQuery
        {
            Query = query,
            Offset = offset,
            Limit = limit,
            NumberOfTopResults = numberOfTopResults
        };
        var result = await _mediator.Send(cmd, cancellationToken);
        return result;
    }

    public async Task<SpotifyAutocompleteResult> Autocomplete(string query, CancellationToken cancellationToken = default)
    {
        const string url =
            "https://spclient.com/searchview/v3/autocomplete";
        var variables = new Dictionary<string, object>
        {
            ["locale"] = "en_US",
            ["request_id"] = Guid.NewGuid().ToString(),
            ["query"] = query,
            ["catalogue"] = "premium",
            ["entity_types"] = "album,artist,genre,playlist,user_profile,track,audio_episode,show",
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ["limit"] = 10,
            ["album_states"] = "live,prerelease"
        };
        var finalUrl = $"{url}?{string.Join("&", variables.Select(kvp => $"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value.ToString())}"))}";


        // Also Content-Type header & App-Platform header
        using var request = new HttpRequestMessage(HttpMethod.Get, finalUrl);
        request.Content = new StringContent("");
        request.Content.Headers.Add("App-Platform", "Android");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var jsonDoc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var hits = jsonDoc.RootElement.GetProperty("hits");
        var outputHits = new SpotifyAutocompleteHit[hits.GetArrayLength()];
        int hits_idx = 0;
        using var hitsEnumerator = hits.EnumerateArray();
        while (hitsEnumerator.MoveNext())
        {
            var hit = hitsEnumerator.Current;
            var uri = SpotifyId.FromUri(hit.GetProperty("uri").GetString());
            var name = hit.GetProperty("name").GetString();
            var image = hit.GetProperty("image_uri").GetString();
            outputHits[hits_idx++] = new SpotifyAutocompleteHit
            {
                Id = uri,
                Name = name,
                ImageUrl = image
            };
        }


        var queries = jsonDoc.RootElement.GetProperty("autocomplete_queries");
        var outputQueries = new SpotifyAutocompleteQuery[queries.GetArrayLength()];
        int queries_idx = 0;
        using var queriesEnumerator = queries.EnumerateArray();
        while (queriesEnumerator.MoveNext())
        {
            var q = queriesEnumerator.Current;
            var queryUri = q.GetProperty("uri").GetString().Replace("spotify:search:", string.Empty);
            var queryDecoded = HttpUtility.UrlDecode(queryUri);

            var segments = q.GetProperty("snippet").GetProperty("segments");
            using var segmentsEnumerator = segments.EnumerateArray();
            var segmentsOutput = new SpotifyAutocompleteQuerySegment[segments.GetArrayLength()];
            int segmentsOutput_idx = 0;
            while (segmentsEnumerator.MoveNext())
            {
                var segment = segmentsEnumerator.Current;
                segmentsOutput[segmentsOutput_idx++] = new SpotifyAutocompleteQuerySegment
                {
                    Value = segment.GetProperty("value").GetString(),
                    Matched = segment.GetProperty("matched").GetBoolean()
                };
            }

            outputQueries[queries_idx++] = new SpotifyAutocompleteQuery
            {
                Query = queryDecoded,
                Segments = segmentsOutput
            };
        }

        return new SpotifyAutocompleteResult
        {
            Hits = outputHits,
            Queries = outputQueries
        };
    }
}

public interface ISpotifySearchClient
{
    Task<SpotifySearchResult> SearchAsync(string query,
        int offset = 0,
        int limit = 10,
        int numberOfTopResults = 5,
        CancellationToken cancellationToken = default);

    Task<SpotifyAutocompleteResult> Autocomplete(string query, CancellationToken cancellationToken = default);
}