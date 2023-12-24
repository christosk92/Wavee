using System.Net.Http.Headers;
using Wavee.Interfaces;

namespace Wavee.Spotify.Infrastructure.HttpClients;

internal class AudioStreamingHttpClient
{
    private readonly HttpClient _httpClient;

    public AudioStreamingHttpClient(HttpClient httpClient, IWaveeCachingProvider? cachingProvider)
    {
        _httpClient = httpClient;
    }


    public async Task<(byte[] Data, long TotalSize)> GetRange(string url, int start, int end, CancellationToken cancellationToken)
    {
        const string akamized = "https://audio4-ak-spotify-com.akamaized.net";
        var finalUrl = url.Replace(akamized, string.Empty);
        using var request = new HttpRequestMessage(HttpMethod.Get, finalUrl);
        request.Headers.Range = new RangeHeaderValue(start, end);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return (bytes, response.Content.Headers.ContentRange?.Length ?? 0);
    }
}