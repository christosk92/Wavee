using System.Net;
using Eum.Spotify.storage;
using Wavee.Interfaces;
using Wavee.Spotify.Core;
using Wavee.Spotify.Core.Clients;
using Wavee.Spotify.Core.Clients.Playback;
using Wavee.Spotify.Core.Clients.Remote;
using Wavee.Spotify.Core.Models.User;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Spotify.Infrastructure.HttpClients;
using Wavee.Spotify.Infrastructure.Services;
using Wavee.Spotify.Infrastructure.Storage;
using Wavee.Spotify.Infrastructure.WebSocket;
using Wavee.Spotify.Interfaces.Clients;
using Wavee.Spotify.Interfaces.Clients.Playback;

namespace Wavee.Spotify;

public sealed class WaveeSpotifyClient : IWaveeSpotifyClient
{
    private WaveeSpotifyClient(ISpotifyTokenClient tokenClient,
        ISpotifyRemoteClient remote,
        ISpotifyTrackClient track,
        ISpotifyPlaybackClient playback)
    {
        Token = tokenClient;
        Remote = remote;
        Track = track;
        Playback = playback;
    }

    public static IWaveeSpotifyClient Create(
        IWaveePlayer player,
        WaveeSpotifyConfig config,
        OAuthCallbackDelegate oAuthCallbackDelegate)
    {
        var tcpClientFactory = new LiveSpotifyTcpClientFactory();
        var sharedClient = new HttpClient();
        var streamingHttpClient = new HttpClient
        {
            BaseAddress = new Uri("https://audio4-ak-spotify-com.akamaized.net")
        };

        var authClient =
            new SpotifyAuthenticationClient(sharedClient);
        var repo = new SpotifyCredentialsRepository(config);
        var authenticationService = new AuthenticationService(oAuthCallbackDelegate,
            repo,
            config,
            authClient
        );
        var apResolverService = new ApResolverService(new ApResolverHttpClient(sharedClient));
        var tcpConnectionService = new TcpConnectionService(apResolverService,
            authenticationService,
            tcpClientFactory);

        var tokenService = new SpotifyTokenService(authClient, tcpConnectionService, repo, config);

        var httpClient = new SpotifyInternalHttpClient(sharedClient, apResolverService, tokenService);
        var webSocketService = new WebSocketService(tokenService,
            new LiveSpotifyWebSocketFactory(),
            apResolverService,
            config,
            httpClient);

        var cache = config.CachingProvider ?? NullCachingService.Instance;

        var remoteClient = new SpotifyRemoteClient(webSocketService, player, config);

        var tokenClient = new SpotifyTokenClient(tokenService);

        var track = new SpotifyTrackClient(tokenService, httpClient);
        var episode = new SpotifyEpisodeClient(tokenService, httpClient);

        var audioKeys = new SpotifyAudioKeyService(tcpConnectionService);

        var audioStreamingClient = new AudioStreamingHttpClient(streamingHttpClient, cache);
        var storageResolveService = new SpotifyStorageResolveService(tokenService, httpClient, audioStreamingClient);


        var playback = new SpotifyPlaybackClient(track,
            episode,
            storageResolveService,
            audioKeys,
            cache,
            config);

        return new WaveeSpotifyClient(tokenClient, remoteClient, track, playback);
    }

    public ISpotifyTokenClient Token { get; }
    public ISpotifyRemoteClient Remote { get; }
    public ISpotifyTrackClient Track { get; }
    public ISpotifyPlaybackClient Playback { get; }
}

public interface IWaveeSpotifyClient
{
    ISpotifyTokenClient Token { get; }
    ISpotifyRemoteClient Remote { get; }
    ISpotifyTrackClient Track { get; }
    ISpotifyPlaybackClient Playback { get; }
}