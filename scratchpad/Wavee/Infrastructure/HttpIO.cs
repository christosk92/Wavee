using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Wavee.Infrastructure;

public static class HttpIO
{
    private static readonly HttpClient Client = new HttpClient();

    public static async Task<T?> GetJsonAsync<T>(string url,
        AuthenticationHeaderValue? auth = null,
        CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        // auth.IfSome(a => request.Headers.Authorization = a);
        if (auth != null)
        {
            request.Headers.Authorization = auth;
        }

        using var response = await Client.SendAsync(request, ct);
        return await response.Content.ReadFromJsonAsync<T>(ct);
    }

    public static async Task<HttpResponseMessage> Put(string url,
        AuthenticationHeaderValue bearerHeader,
        IReadOnlyDictionary<string, string> headers,
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

        return await Client.SendAsync(request, ct);
    }

    public static async Task<HttpResponseMessage> Get(
        string url,
        IReadOnlyDictionary<string, string> headers,
        AuthenticationHeaderValue? bearerHeader = null, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        //bearerHeader.IfSome(b => request.Headers.Authorization = b);
        if (bearerHeader != null)
        {
            request.Headers.Authorization = bearerHeader;
        }

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

        var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

        
        return response;
    }

    public static async Task<HttpResponseMessage> GetWithContentRange(
        string url,
        int start,
        int end,
        IReadOnlyDictionary<string, string> headers,
        AuthenticationHeaderValue? bearerHeader = null, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        //bearerHeader.IfSome(b => request.Headers.Authorization = b);
        if (bearerHeader != null)
        {
            request.Headers.Authorization = bearerHeader;
        }

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

        request.Headers.Range = new RangeHeaderValue(start, end);
        var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        return response;
    }

    public static async Task<HttpResponseMessage> Post(string url,
        IReadOnlyDictionary<string, string> headers,
        HttpContent body,
        AuthenticationHeaderValue? bearerHeader = null, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (bearerHeader != null)
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

        return await Client.SendAsync(request, ct);
    }
}