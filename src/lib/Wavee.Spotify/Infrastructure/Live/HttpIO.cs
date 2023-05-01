using System.Net.Http.Headers;
using LanguageExt.UnsafeValueAccess;

namespace Wavee.Spotify.Infrastructure.Live;

public readonly struct HttpIO : Infrastructure.Traits.HttpIO
{
    private readonly HttpClient _httpClient;

    public HttpIO(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async ValueTask<HttpResponseMessage> PutAsync(string url,
        HttpContent content,
        Option<HashMap<string, string>> headers,
        Option<AuthenticationHeaderValue> auth,
        CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = content
        };
        

        if (auth.IsSome)
        {
            request.Headers.Authorization = auth.ValueUnsafe();
        }
        if (headers.IsSome)
        {
            foreach (var header in headers.ValueUnsafe())
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }
        var response = await _httpClient.SendAsync(request, ct);
        return response;
    }
}