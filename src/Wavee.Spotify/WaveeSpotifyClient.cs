using Wavee.Spotify.Common;
using Wavee.Spotify.Core;
using Wavee.Spotify.Core.Clients;
using Wavee.Spotify.Core.Clients.Remote;
using Wavee.Spotify.Core.Interfaces;
using Wavee.Spotify.Core.Interfaces.Clients;
using Wavee.Spotify.Core.Models.User;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Spotify.Infrastructure.HttpClients;
using Wavee.Spotify.Infrastructure.Services;
using Wavee.Spotify.Infrastructure.Storage;
using Wavee.Spotify.Infrastructure.WebSocket;

namespace Wavee.Spotify;

public sealed class WaveeSpotifyClient : IWaveeSpotifyClient
{
    private WaveeSpotifyClient(ISpotifyTokenClient tokenClient, ISpotifyRemoteClient remote, ISpotifyTrackClient track)
    {
        Token = tokenClient;
        Remote = remote;
        Track = track;
    }

    public static IWaveeSpotifyClient Create(
        WaveeSpotifyConfig config,
        OAuthCallbackDelegate oAuthCallbackDelegate)
    {
        var tcpClientFactory = new LiveSpotifyTcpClientFactory();

        var authClient =
            new SpotifyAuthenticationClient(new HttpClient());
        var repo = new SpotifyCredentialsRepository(config);
        var authenticationService = new AuthenticationService(oAuthCallbackDelegate,
            repo,
            config,
            authClient
        );
        var apResolverService = new ApResolverService(new ApResolverHttpClient(new HttpClient()));
        var tcpConnectionService = new TcpConnectionService(apResolverService,
            authenticationService,
            tcpClientFactory);

        var tokenService = new SpotifyTokenService(authClient, tcpConnectionService, repo, config);
        
        var httpClient = new SpotifyInternalHttpClient(new HttpClient(), apResolverService, tokenService);
        var webSocketService = new WebSocketService(tokenService,
            new LiveSpotifyWebSocketFactory(),
            apResolverService,
            config,
            httpClient);
        
        var remoteClient = new SpotifyRemoteClient(webSocketService);
        var tokenClient = new SpotifyTokenClient(tokenService);
        var track = new SpotifyTrackClient(tokenService, httpClient);
        return new WaveeSpotifyClient(tokenClient, remoteClient, track);
    }

    public ISpotifyTokenClient Token { get; }
    public ISpotifyRemoteClient Remote { get; }
    public ISpotifyTrackClient Track { get; }
}

public interface IWaveeSpotifyClient
{
    ISpotifyTokenClient Token { get; }
    ISpotifyRemoteClient Remote { get; }
    ISpotifyTrackClient Track { get; }
}