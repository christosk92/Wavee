using System;
using System.Net.Http;
using Refit;
using Wavee.Contracts.Interfaces;
using Wavee.Contracts.Interfaces.Clients;
using Wavee.UI.Spotify.Auth;
using Wavee.UI.Spotify.Clients;
using Wavee.UI.Spotify.ContentSerializers;
using Wavee.UI.Spotify.Interfaces.Api;
using Wavee.UI.Spotify.Json;
using Wavee.UI.Spotify.Playback;

namespace Wavee.UI.Spotify;

public sealed class SpotifyClient : IAccountClient
{
    public SpotifyClient(SpotifyConfig config)
    {
        var tokensClient = new SpotifyTokenClient(config.Auth, config.DeviceId);
        var contentSerializer = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            Converters =
            {
                new System.Text.Json.Serialization.JsonStringEnumConverter(),
                new HomeItemSerializer(),
                new HomeSectionSerializer()
            }
        };

        var httpClientHandler = new HttpClientHandler();
        var customHandler = new CustomAuthHandler(tokensClient)
        {
            InnerHandler = httpClientHandler
        };
        var httpClient = new HttpClient(customHandler)
        {
            BaseAddress = new Uri(SpotifyUrls.Partner.BaseUrl)
        };

        var partnerApi = RestService.For<ISpotifyPartnerApi>(httpClient, new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(contentSerializer)
        });

        var gzipDecompressionHandler = new GzipDecompressionHandler(customHandler);
        var gzipCompressionHandler = new GzipCompressionHandler(gzipDecompressionHandler);
        var refitSettings = new RefitSettings
        {
            ContentSerializer = new ProtobufContentSerializer()
        };
        var spHttpClient = new HttpClient(gzipCompressionHandler)
        {
            BaseAddress = new Uri("https://gae2-spclient.spotify.com")
        };
        var spClient = RestService.For<ISpClient>(spHttpClient, refitSettings);
        var connectionFactory = new SpotifyWebsocketConnectionFactory();

        Home = new SpotifyHomeClient(partnerApi);
        Tracks = new SpotifyTrackClient(spClient);
        Episodes = new SpotifyEpisodeClient(spClient);


        Playback = new SpotifyPlaybackClient(spClient, tokensClient, config.DeviceId, this, DeviceConnected,
            connectionFactory, config.Player);
    }

    public IHomeClient Home { get; }
    public IColorClient Color { get; }
    public IPlaybackClient Playback { get; }
    public ITrackClient Tracks { get; }
    public IEpisodeClient Episodes { get; }

    private void DeviceConnected((SpotifyPlaybackDevice Old, SpotifyPlaybackDevice New) obj)
    {
        //TODO
        if (obj.Old is not null)
        {
        }

        if (obj.New is not null)
        {
            // Setup library websocket events
        }
    }
}

public sealed class SpotifyClientFactory : IAccountClientFactory
{
    private readonly SpotifyConfig _config;

    public SpotifyClientFactory(SpotifyConfig config)
    {
        _config = config;
    }

    public IAccountClient Create()
    {
        return new SpotifyClient(_config);
    }
}