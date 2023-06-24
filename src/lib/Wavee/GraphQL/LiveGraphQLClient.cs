using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using Wavee.Infrastructure;
using Wavee.Metadata.Artist;

namespace Wavee.GraphQL;

internal readonly struct LiveGraphQLClient
{
    private const string BaseUrl = "https://api-partner.spotify.com/pathfinder/v1/query";
    private static readonly ConcurrentDictionary<Type, string> _hashes = new();
    private readonly Func<CancellationToken, ValueTask<string>> _fetchAccessTokenFactory;

    public LiveGraphQLClient(Func<CancellationToken, ValueTask<string>> fetchAccessTokenFactory)
    {
        _fetchAccessTokenFactory = fetchAccessTokenFactory;
    }

    private static IReadOnlyDictionary<string, string> _empty = new Dictionary<string, string>();

    public async Task<HttpResponseMessage> Query(IGraphQLQuery query, CancellationToken cancellationToken = default)
    {
        // var hash = _hashes.GetOrAdd(query.GetType(),
        //     ComputeHash(query.));
        var hash = query.Operationhash;
        //https://api-partner.spotify.com/pathfinder/v1/query?operationName=queryArtistOverview&variables=%7B%22uri%22%3A%22spotify%3Aartist%3A4XQhU3S4TyPkiPIsSu2hmA%22%2C%22locale%22%3A%22%22%2C%22includePrerelease%22%3Afalse%7D&extensions=%7B%22persistedQuery%22%3A%7B%22version%22%3A1%2C%22sha256Hash%22%3A%2235648a112beb1794e39ab931365f6ae4a8d45e65396d641eeda94e4003d41497%22%7D%7D
        var token = await _fetchAccessTokenFactory(cancellationToken);
        var header = new AuthenticationHeaderValue("Bearer", token);
        var variablesJson = JsonSerializer.Serialize(query.Variables);
        var urlEncoded = HttpUtility.UrlEncode(variablesJson);
        var url =
            $"{BaseUrl}?operationName={query.OperationName}&variables={urlEncoded}&extensions=%7B%22persistedQuery%22%3A%7B%22version%22%3A1%2C%22sha256Hash%22%3A%22{hash}%22%7D%7D";
        var response = await HttpIO.Get(url, _empty, header, cancellationToken);
        return response;
    }

    private static string ComputeHash(string queryQuery)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(queryQuery));
        var sb = new StringBuilder();
        foreach (var b in hash)
        {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }
}