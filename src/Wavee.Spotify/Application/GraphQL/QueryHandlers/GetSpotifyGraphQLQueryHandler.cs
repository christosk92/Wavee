using System.Text.Json;
using Mediator;
using Wavee.Spotify.Application.GraphQL.Queries;

namespace Wavee.Spotify.Application.GraphQL.QueryHandlers;

public sealed class GetSpotifyGraphQLQueryHandler : IQueryHandler<GetSpotifyGraphQLQuery, HttpResponseMessage>
{
    private readonly HttpClient _httpClient;

    public GetSpotifyGraphQLQueryHandler(IHttpClientFactory httpClient)
    {
        _httpClient = httpClient.CreateClient(Constants.SpotifyPartnerApiHttpClient);
    }
    public async ValueTask<HttpResponseMessage> Handle(GetSpotifyGraphQLQuery query, CancellationToken cancellationToken)
    {
        const string path = "query";
        var variablesAsJson = JsonSerializer.Serialize(query.Variables);
        var extensions = new Dictionary<string, object>
        {
            {
                "persistedQuery", new Dictionary<string, object>
                {
                    { "version", 1 },
                    { "sha256Hash", query.Hash }
                }
            }
        };
        var extensionsAsJson = JsonSerializer.Serialize(extensions);
        var uri = $"{path}?" +
                  $"operationName={query.OperationName}&variables={variablesAsJson}&extensions={extensionsAsJson}";
        var response = await _httpClient.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();
        return response;
    }
}