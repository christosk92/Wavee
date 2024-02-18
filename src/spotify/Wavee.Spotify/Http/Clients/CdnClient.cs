using System.Net.Http.Headers;
using Eum.Spotify.storage;
using Google.Protobuf;
using Wavee.Spotify.Extensions;
using Wavee.Spotify.Http.Interfaces;
using Wavee.Spotify.Http.Interfaces.Clients;
using Wavee.Spotify.Playback;

namespace Wavee.Spotify.Http.Clients;

internal sealed class CdnClient : ApiClient, ICdnClient
{
    public CdnClient(IAPIConnector apiConnector) : base(apiConnector)
    {
    }

    public Task<HttpResponseMessage> StreamFromCdnAsync(string cdnUrl, long offset, long length)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, cdnUrl);
        request.Headers.Range = new RangeHeaderValue(offset, offset + length - 1);
        return Api.SendAsync(request, CancellationToken.None);
    }

    public Task<StorageResolveResponse> GetAudioStorageAsync(ByteString fileId)
    {
        var base16 = fileId.Span.ToBase16();
        var endpoint = SpotifyUrls.Cdn.AudioStorage(base16);

        return Api.Get<StorageResolveResponse>(new Uri(endpoint), CancellationToken.None);
    }
}