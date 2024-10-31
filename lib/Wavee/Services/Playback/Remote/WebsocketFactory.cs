using Microsoft.Extensions.Logging;
using Wavee.Interfaces;

namespace Wavee.Services.Playback.Remote;

internal sealed class WebsocketFactory : IWebsocketFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ApResolver _apResolver;
    private readonly ISpotifyTokenClient _tokenClient;
    public WebsocketFactory(ApResolver apResolverValue, ISpotifyTokenClient tokenValue, ILoggerFactory loggerFactory)
    {
        _apResolver = apResolverValue;
        _tokenClient = tokenValue;
        _loggerFactory = loggerFactory;
    }

    public async Task<ISpotifyWebsocket> CreateWebsocket(CancellationToken token)
    {
        if (_host is null)
        {
            var dealerHost = await _apResolver.ResolveAsync("dealer", token);
            _host = dealerHost;
        }

        var (host, port) = _host.Value;
        var bearer = await _tokenClient.GetBearerToken(token);
        var url = $"wss://{host}:{port}?access_token={bearer.Value}";
        var ws =  new SpotifyWebsocket(url, _loggerFactory.CreateLogger<SpotifyWebsocket>());
        await ws.ConnectAsync(token);
        return ws;
    }

    private (string, int)? _host;
}