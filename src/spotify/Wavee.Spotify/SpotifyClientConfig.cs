using Wavee.Core;
using Wavee.Core.Extensions;
using Wavee.Spotify.Authenticators;
using Wavee.Spotify.Http;
using Wavee.Spotify.Http.Interfaces;
using Wavee.Spotify.Http.Serializers;

namespace Wavee.Spotify;

public sealed class SpotifyClientConfig
{
    public SpotifyClientConfig(
        ISpotifyCache cache,
        IWaveePlayer player,
        IAuthenticator? authenticator,
        IJsonSerializer jsonSerializer,
        IProtobufDeserializer protobufDeserializer,
        IHttpClient httpClient,
        IRetryHandler? retryHandler,
        IHttpLogger? httpLogger,
        IPaginator defaultPaginator,
        IAPIConnector? apiConnector = null
    )
    {
        Cache = cache;
        Player = player;
        Authenticator = authenticator;
        JsonSerializer = jsonSerializer;
        ProtobufDeserializer = protobufDeserializer;
        HttpClient = httpClient;
        RetryHandler = retryHandler;
        HttpLogger = httpLogger;
        DefaultPaginator = defaultPaginator;
        ApiConnector = apiConnector;
    }

    public SpotifyClientConfig WithAuthenticator(IAuthenticator authenticator)
    {
        Guard.NotNull(nameof(authenticator), authenticator);

        return new SpotifyClientConfig(
            Cache,
            Player,
            authenticator,
            JsonSerializer,
            ProtobufDeserializer,
            HttpClient,
            RetryHandler,
            HttpLogger,
            DefaultPaginator
        );
    }

    public IAuthenticator? Authenticator { get; private set; }
    public IJsonSerializer JsonSerializer { get; private set; }
    public IProtobufDeserializer ProtobufDeserializer { get; private set; }
    public IHttpClient HttpClient { get; private set; }
    public IHttpLogger? HttpLogger { get; private set; }
    public IRetryHandler? RetryHandler { get; private set; }
    public IPaginator DefaultPaginator { get; private set; }
    public IAPIConnector? ApiConnector { get; private set; }
    public string DeviceId { get; } = Guid.NewGuid().ToString("N");
    public IWaveePlayer Player { get; private set; }
    public ISpotifyCache Cache { get; private set; }

    public IAPIConnector BuildApiConnector()
    {
        return ApiConnector ?? new ApiConnector(
            Authenticator,
            JsonSerializer,
            ProtobufDeserializer,
            HttpClient,
            RetryHandler,
            HttpLogger,
            DeviceId
        );
    }

    public static SpotifyClientConfig CreateDefault(IWaveePlayer? player = null, ISpotifyCache? cache = null)
    {
        return new SpotifyClientConfig(
            cache ?? SpotifyInMemoryCache.Instance,
            player ?? new WaveePlayer(),
            null,
            new SystemTextJsonSerializer(),
            new ProtobufDeserializer(),
            new NetHttpClient(),
            null,
            null,
            new SimplePaginator()
        );
    }
}