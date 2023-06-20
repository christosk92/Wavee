using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using Wavee.Infrastructure;

namespace Wavee.Spotify.InternalApi.Live;

internal readonly struct LiveInternalApi : IInternalApi
{
    private const string partnerApi =
        "https://api-partner.spotify.com/pathfinder/v1/query?operationName={0}&variables={1}&extensions=%7B%22persistedQuery%22%3A%7B%22version%22%3A1%2C%22sha256Hash%22%3A%22{2}%22%7D%7D";

    private readonly Func<CancellationToken, ValueTask<string>> _tokenFactory;

    public LiveInternalApi(Func<CancellationToken, ValueTask<string>> tokenFactory)
    {
        _tokenFactory = tokenFactory;
    }

    public async Task<HttpResponseMessage> GetPartner(string operationName, string operationHash,
        object? variables = null,
        CancellationToken ct = default)
    {
        var variablesJson = JsonSerializer.Serialize(variables);
        var finalUrl = string.Format(partnerApi, operationName, HttpUtility.UrlEncode(variablesJson), operationHash);
        return await GetResponse(finalUrl, ct);
    }

    private static IReadOnlyDictionary<string, string> empty = new Dictionary<string, string>();

    private async Task<HttpResponseMessage> GetResponse(string url, CancellationToken ct)
    {
        var bearer = await _tokenFactory(ct);
        var bearerHeader = new AuthenticationHeaderValue("Bearer", bearer);
        return await HttpIO.Get(url, empty, bearerHeader, ct);
    }
}