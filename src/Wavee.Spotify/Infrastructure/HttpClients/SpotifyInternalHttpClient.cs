using System.Net.Http.Headers;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using Wavee.Spotify.Core.Utils;
using Wavee.Spotify.Infrastructure.Services;
using Wavee.Spotify.Interfaces;

namespace Wavee.Spotify.Infrastructure.HttpClients;

internal sealed class SpotifyInternalHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly IApResolverService _apResolverService;
    private readonly SpotifyTokenService _tokenService;
    public SpotifyInternalHttpClient(HttpClient httpClient, 
        IApResolverService apResolverService,
        SpotifyTokenService tokenService)
    {
        _httpClient = httpClient;
        _apResolverService = apResolverService;
        _tokenService = tokenService;
    }

    public async Task<HttpResponseMessage> Get(string endpoint, 
        string? accept = null,
        CancellationToken cancellationToken = default)
    {
        var hostUrl = await _apResolverService.GetSpClient(cancellationToken);
        var url = $"{hostUrl}{endpoint}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (accept is not null)
        {
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));
        }
        
        var token = await _tokenService.GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return await _httpClient.SendAsync(request, cancellationToken);
    }
    
    public async Task<Cluster> PutStateAsync(string connectionId, PutStateRequest localState, CancellationToken cancellationToken)
    {
        // TODO (massive): GZIP !!!!!!!!!!!!!!!!!
        
        var hostUrl = await _apResolverService.GetSpClient(cancellationToken);
        var putstateUrl = $"{hostUrl}/connect-state/v1/devices/{localState.Device.DeviceInfo.DeviceId}";
        using var request = new HttpRequestMessage(HttpMethod.Put, putstateUrl);
        request.Headers.Add("X-Spotify-Connection-Id", connectionId);
        
        // Auth
        var token = await _tokenService.GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        //accept-encoding: gzip
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        
        using var byteArrayContent = new ByteArrayContent(localState.ToByteArray());
        byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/protobuf");
        //  byteArrayContent.Headers.ContentEncoding.Add("gzip");   
        request.Content = byteArrayContent;
        
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        ReadOnlyMemory<byte> data = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var cluster = Cluster.Parser.ParseFrom(Gzip.UnsafeDecompressAlt(data.Span));
        return cluster;
    }
}