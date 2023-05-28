using System.Net.Http.Headers;
using LanguageExt;

namespace Wavee.Core.Infrastructure.IO;

public static class HttpIO
{
    private static readonly HttpClient _httpClient = new();

    public static async Task<HttpResponseMessage> GetAsync(string url,
        Option<AuthenticationHeaderValue> bearerHeader,
        HashMap<string, string> headers,
        CancellationToken ct = default)
    {
        // return await _httpClient.GetAsync(url, ct);
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = bearerHeader.IfNoneUnsafe(() => null);
        foreach (var header in headers)
        {
            switch (header.Key)
            {
                case "accept":
                    request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue(header.Value));
                    break;
                default:
                    request.Headers.Add(header.Key, header.Value);
                    break;
            }
        }

        return await _httpClient.SendAsync(request, ct);
    }

    public static async Task<HttpResponseMessage> Put(string url,
        AuthenticationHeaderValue bearerHeader,
        HashMap<string, string> headers,
        HttpContent body, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, url);
        request.Headers.Authorization = bearerHeader;
        foreach (var header in headers)
        {
            switch (header.Key)
            {
                case "accept":
                    request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue(header.Value));
                    break;
                default:
                    request.Headers.Add(header.Key, header.Value);
                    break;
            }
        }

        request.Content = body;

        return await _httpClient.SendAsync(request, ct);
    }

    public static async Task<HttpResponseMessage> GetWithContentRange(string url, int start, int length, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        var end = start + length - 1;
        request.Headers.Range = new RangeHeaderValue(start, end);
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        return response;
    }

    public static async Task<HttpResponseMessage> Post(string url,
        AuthenticationHeaderValue bearer, 
        HttpContent content, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = bearer;
        request.Content = content;
        return await _httpClient.SendAsync(request, ct);
    }
}