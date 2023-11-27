using System.Net.Http.Headers;
using Mediator;
using Wavee.Spotify.Application.Decrypt;
using Wavee.Spotify.Application.Playback.Queries;

namespace Wavee.Spotify.Application.Playback.QueryHandlers;

public sealed class SpotifyGetChunkQueryHandler : IQueryHandler<SpotifyGetChunkQuery, (byte[] Data, long TotalSize)>
{
    private readonly HttpClient _httpClient;

    public SpotifyGetChunkQueryHandler(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(Constants.SpotifyCdnPlaybackClientName);
    }

    public async ValueTask<(byte[] Data, long TotalSize)> Handle(SpotifyGetChunkQuery query,
        CancellationToken cancellationToken)
    {
        const string akamized = "https://audio4-ak-spotify-com.akamaized.net";
        var url = query.CdnUrl!.Replace(akamized, string.Empty);
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        var start = query.Index * SpotifyUnoffsettedStream.ChunkSize;
        var end = start + SpotifyUnoffsettedStream.ChunkSize - 1;
        request.Headers.Range = new RangeHeaderValue(start, end);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return (bytes, response.Content.Headers.ContentRange.Length.Value);
    }
}