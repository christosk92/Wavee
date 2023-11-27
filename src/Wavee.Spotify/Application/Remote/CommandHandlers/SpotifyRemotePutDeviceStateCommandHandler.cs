using System.Net.Http.Headers;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using Mediator;
using Wavee.Spotify.Application.Remote.Commands;
using Wavee.Spotify.Utils;

namespace Wavee.Spotify.Application.Remote.CommandHandlers;

public sealed class
    SpotifyRemotePutDeviceStateCommandHandler : ICommandHandler<SpotifyRemotePutDeviceStateCommand, Cluster>
{
    private readonly IMediator _mediator;
    private readonly HttpClient _httpClient;

    public SpotifyRemotePutDeviceStateCommandHandler(IHttpClientFactory httpClientFactory, IMediator mediator)
    {
        _mediator = mediator;
        _httpClient = httpClientFactory.CreateClient(Constants.SpotifyRemoteStateHttpClietn);
    }

    public async ValueTask<Cluster> Handle(SpotifyRemotePutDeviceStateCommand command, CancellationToken cancellationToken)
    {
        // TODO (massive): GZIP !!!!!!!!!!!!!!!!!
        
        var putstateUrl = $"https://spclient.com/connect-state/v1/devices/{command.State.Device.DeviceInfo.DeviceId}";
        using var request = new HttpRequestMessage(HttpMethod.Put, putstateUrl);
        request.Headers.Add("X-Spotify-Connection-Id", command.ConnectionId);
        //accept-encoding: gzip
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        
        using var byteArrayContent = new ByteArrayContent(command.State.ToByteArray());
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