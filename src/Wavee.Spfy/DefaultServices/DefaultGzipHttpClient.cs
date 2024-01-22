using System.Net;
using System.Net.Http.Headers;
using Eum.Spotify.connectstate;
using Google.Protobuf;

namespace Wavee.Spfy.DefaultServices;

internal sealed class DefaultGzipHttpClient : IGzipHttpClient
{
    private static readonly HttpClient sharedClient = new HttpClient(new HttpClientHandler
    {
        AutomaticDecompression = DecompressionMethods.All
    });

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return sharedClient.SendAsync(request, cancellationToken);
    }

    public async Task<Cluster> PutState(
        IHttpClient httpClient,
        Func<ValueTask<string>> tokenFactory,
        string connectionId, 
        PutStateRequest putState,
        CancellationToken cancellationToken = default)
    {
        // TODO (massive): GZIP !!!!!!!!!!!!!!!!!
        var spClient = await ApResolve.GetSpClient(httpClient);

        var putstateUrl = $"https://{spClient}/connect-state/v1/devices/{putState.Device.DeviceInfo.DeviceId}";
        using var request = new HttpRequestMessage(HttpMethod.Put, putstateUrl);
        request.Headers.Add("X-Spotify-Connection-Id", connectionId);

        // Auth
        var token = await tokenFactory();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        //accept-encoding: gzip
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

        using var byteArrayContent = new ByteArrayContent(putState.ToByteArray());
        byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/protobuf");
        //  byteArrayContent.Headers.ContentEncoding.Add("gzip");   
        request.Content = byteArrayContent;

        using var response = await sharedClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var cluster = Cluster.Parser.ParseFrom(stream);
        //
        // ReadOnlyMemory<byte> data = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        // var cluster = Cluster.Parser.ParseFrom(Gzip.UnsafeDecompressAlt(data.Span));
        return cluster;
    }
}