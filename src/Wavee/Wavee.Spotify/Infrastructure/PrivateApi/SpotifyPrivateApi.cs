using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Spotify.Collection.Proto.V2;
using Wavee.Infrastructure.IO;
using Wavee.Spotify.Infrastructure.PrivateApi.Contracts;
using Wavee.Spotify.Infrastructure.PrivateApi.Contracts.Response;

namespace Wavee.Spotify.Infrastructure.PrivateApi;

internal readonly struct SpotifyPrivateApi : ISpotifyPrivateApi
{
    private const string partnerApi =
        "https://api-partner.spotify.com/pathfinder/v1/query?operationName={0}&variables={1}&extensions=%7B%22persistedQuery%22%3A%7B%22version%22%3A1%2C%22sha256Hash%22%3A%22{2}%22%7D%7D";

    private readonly Func<CancellationToken, Task<string>> _tokenFactory;

    public SpotifyPrivateApi(Func<CancellationToken, Task<string>> tokenFactory)
    {
        _tokenFactory = tokenFactory;
    }

    public async Task<SpotifyColors> FetchColorFor(Seq<string> artwork, CancellationToken ct = default)
    {
        const string operationHash = "d7696dd106f3c84a1f3ca37225a1de292e66a2d5aced37a66632585eeb3bbbfa";
        const string operationName = "fetchExtractedColors";
        var variables = new
        {
            uris = artwork
        };
        var variablesJson = JsonSerializer.Serialize(variables);

        //http encode
        var finalUrl = string.Format(partnerApi, operationName, HttpUtility.UrlEncode(variablesJson), operationHash);
        using var response = await GetResponse(finalUrl, ct);

        /*
         * {
    "data": {
        "extractedColors": [
            {
                "__typename": "ExtractedColors",
                "colorRaw": {
                    "hex": "#488098",
                    "isFallback": false
                },
                "colorDark": {
                    "hex": "#477E95",
                    "isFallback": false
                },
                "colorLight": {
                    "hex": "#488098",
                    "isFallback": false
                }
            }
        ]
    },
    "extensions": {
        "cacheControl": {
            "version": 1.0,
            "hints": []
        }
    }
}
         */
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = json.RootElement;
        //if errors is not null, then there was an error
        if (root.TryGetProperty("errors", out var errors))
        {
            var error = errors.EnumerateArray().First();
            var message = error.GetProperty("message").GetString();
            throw new Exception(message);
        }

        var data = root.GetProperty("data");
        var extractedColors = data.GetProperty("extractedColors");
        var first = extractedColors.EnumerateArray().First();
        var colorDark = first.GetProperty("colorDark").GetProperty("hex").GetString();
        var colorLight = first.GetProperty("colorLight").GetProperty("hex").GetString();

        return new SpotifyColors(Dark: colorDark!, Light: colorLight!);
    }

    public async Task<Unit> WriteLibrary(WriteRequest writeRequest, CancellationToken ct = default)
    {
        var spClient = ApResolve.ApResolver.SpClient.ValueUnsafe();
        var url = $"https://{spClient}/collection/v2/write";
        var data = writeRequest.ToByteArray();
        using var streamContent = new ByteArrayContent(data);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.collection-v2.spotify.proto");
       
        using var response = await PostResponse(url, streamContent, ct);
        response.EnsureSuccessStatusCode();

        return Unit.Default;
    }

    private async Task<HttpResponseMessage> GetResponse(string url, CancellationToken ct)
    {
        var bearer = await _tokenFactory(ct);
        var bearerHeader = new AuthenticationHeaderValue("Bearer", bearer);
        return await HttpIO.Get(url, bearerHeader, HashMap<string, string>.Empty, ct);
    }

    private async Task<HttpResponseMessage> PostResponse(string url, HttpContent body, CancellationToken ct)
    {
        var bearer = await _tokenFactory(ct);
        var bearerHeader = new AuthenticationHeaderValue("Bearer", bearer);
        return await HttpIO.Post(url, bearerHeader, HashMap<string, string>.Empty, body, ct);
    }
}