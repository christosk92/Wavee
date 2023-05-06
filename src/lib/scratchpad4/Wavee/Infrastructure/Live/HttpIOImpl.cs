using System.Net.Http.Headers;
using LanguageExt.UnsafeValueAccess;
using Wavee.Infrastructure.Traits;

namespace Wavee.Infrastructure.Live;

internal sealed class HttpIOImpl : HttpIO
{
    public static readonly HttpIO Default = new HttpIOImpl();
    private readonly HttpClient _client = new HttpClient();

    public async ValueTask<HttpResponseMessage> Get(string url, Option<AuthenticationHeaderValue> authentication,
        Option<HashMap<string, string>> headers, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (authentication.IsSome)
        {
            request.Headers.Authorization = authentication.ValueUnsafe();
        }

        if (headers.IsSome)
        {
            foreach (var (key, value) in headers.ValueUnsafe())
            {
                request.Headers.Add(key, value);
            }
        }

        var response = await _client.SendAsync(request, ct);
        return response;
    }

    public async ValueTask<HttpResponseMessage> GetWithContentRange(string url, int start, int length,
        CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        var end = start + length - 1;
        request.Headers.Range = new RangeHeaderValue(start, end);

        var response = await _client.SendAsync(request, ct);
        return response;
    }

    public async ValueTask<HttpResponseMessage> Put(
        string url,
        Option<AuthenticationHeaderValue> authheader,
        Option<HashMap<string, string>> headers,
        HttpContent content,
        CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, url);
        if (authheader.IsSome)
        {
            request.Headers.Authorization = authheader.ValueUnsafe();
        }

        if (headers.IsSome)
        {
            foreach (var (key, value) in headers.ValueUnsafe())
            {
                request.Headers.Add(key, value);
            }
        }

        request.Content = content;
        var response = await _client.SendAsync(request, ct);
        return response;
    }
}