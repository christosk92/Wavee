using System.Net.Http.Headers;
using Eum.Spotify.storage;
using Google.Protobuf;
using Wavee.Spotify.Application.AudioKeys.QueryHandlers;

namespace Wavee.Spotify.Application.StorageResolve;

internal sealed class SpotifyStorageResolver : ISpotifyStorageResolver
{
    private readonly HttpClient _httpClient;

    public SpotifyStorageResolver(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(Constants.SpotifyRemoteStateHttpClietn);
    }

    public async Task<string> GetStreamingUrl(ByteString fileFileId, CancellationToken cancellationToken)
    {
        const string url = "https://spclient.com/storage-resolve/files/audio/interactive/{0}";
        var hexId = SpotifyGetAudioKeyQueryHandler.ToBase16(fileFileId.ToByteArray());
        var audioUrl = string.Format(url, hexId);
        using var request = new HttpRequestMessage(HttpMethod.Get, audioUrl);
        //accept protobuf
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/protobuf"));
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var res = StorageResolveResponse.Parser.ParseFrom(stream);
        return res.Cdnurl.First();
    }
}

public interface ISpotifyStorageResolver
{
    Task<string> GetStreamingUrl(ByteString fileFileId, CancellationToken cancellationToken);
}