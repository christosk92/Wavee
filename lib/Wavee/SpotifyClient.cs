using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;
using Wavee.Config;
using Wavee.HttpHandlers;
using Wavee.Interfaces;
using Wavee.Models.Playlist;
using Wavee.Models.Remote;
using Wavee.Playback.Player;
using Wavee.Repositories;
using Wavee.Services;
using Wavee.Services.Playback;
using Wavee.Services.Playback.Remote;
using Wavee.Services.Playlists;
using Wavee.Services.Session;

namespace Wavee;

/// <summary>
/// Implementation of the <see cref="ISpotifyClient"/> interface for managing Spotify services.
/// </summary>
public sealed partial class SpotifyClient : ISpotifyClient
{
    private readonly Lazy<SpotifySessionHolder> _sessionHolder;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpotifyClient"/> class.
    /// </summary>
    /// <param name="config">Configuration settings for Spotify client.</param>
    /// <param name="loggerFactory">An optional logger factory for creating loggers. If null, a null logger factory is used.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="config"/> is null.</exception>
    public SpotifyClient(
        IWaveePlayer player,
        SpotifyConfig config, ILoggerFactory? loggerFactory = null)
    {
        Player = player;
        player.Config = config;
        if (config == null)
            throw new ArgumentNullException(nameof(config), "SpotifyConfig cannot be null.");

        loggerFactory ??= NullLoggerFactory.Instance;

        HttpClient = CreateHttpClient(config, loggerFactory);
        ApResolver =
            new Lazy<ApResolver>(() => new ApResolver(HttpClient, loggerFactory.CreateLogger<ApResolver>()));
        IOAuthClient oauthClient = new OAuthClient(HttpClient, loggerFactory.CreateLogger<OAuthClient>());
        ITcpClientFactory tcpClientFactory = new TcpClientFactory(loggerFactory);

        _sessionHolder = new(() => new SpotifySessionHolder(
            ApResolver.Value,
            config, HttpClient,
            oauthClient,
            tcpClientFactory,
            loggerFactory));

        Token = new Lazy<ISpotifyTokenClient>(() =>
            new SpotifyTokenClient(_sessionHolder.Value, HttpClient, config,
                loggerFactory.CreateLogger<SpotifyTokenClient>()));
        AuthenticatedHttpClient =
            new Lazy<HttpClient>(() => CreateHttpClient(config, loggerFactory, Token.Value, ApResolver.Value));
        Api = new Lazy<ISpotifyApiClient>(() =>
        {
            var cacheRepository = new SqliteCacheRepository<string>(config.Cache.Location,
                loggerFactory.CreateLogger<SqliteCacheRepository<string>>());
            var cacheKeyBuilder = new DefaultCacheKeyBuilder();
            var cachingService = new CachingService(cacheRepository, loggerFactory.CreateLogger<CachingService>());
            var libraryRepo = new SqliteLibraryRepository(config.Cache.Location,
                loggerFactory.CreateLogger<SqliteLibraryRepository>());
            return new SpotifyApiClient(config, AuthenticatedHttpClient.Value,
                loggerFactory.CreateLogger<SpotifyApiClient>(),
                cachingService,
                cacheRepository,
                _sessionHolder.Value,
                libraryRepo
            );
        });
        AudioKeyManager = new Lazy<IAudioKeyManager>(() =>
            new PlayPlayAudioKeyManager(Api.Value, loggerFactory.CreateLogger<IAudioKeyManager>()));
        TimeProvider = new SpotifyTimeProvider(ApiClient);
        Player.TimeProvider = TimeProvider;
        IWebsocketFactory websocketFactory = new WebsocketFactory(ApResolver.Value, Token.Value, loggerFactory);
        WebsocketState = new Lazy<ISpotifyWebsocketState>(() =>
            new SpotifyWebsocketState(
                config,
                Api.Value,
                websocketFactory,
                loggerFactory.CreateLogger<SpotifyWebsocketState>(),
                Player));


        Playlist = new Lazy<ISpotifyPlaylistClient>(() =>
        {
            var playlistRepo = new SqliteSpotifyPlaylistRepository(config.Cache.Location,
                loggerFactory.CreateLogger<SqliteSpotifyPlaylistRepository>());
            return new SpotifyPlaylistClient(
                Api.Value,
                WebsocketState.Value,
                playlistRepo,
                loggerFactory);
        });
        Playback =
            new SpotifyPlaybackClient(
                config,
                Player,
                WebsocketState.Value,
                Api.Value,
                AudioKeyManager.Value,
                _sessionHolder.Value,
                loggerFactory,
                PlaylistClient,
                TimeProvider);

        Library = new Lazy<ISpotifyLibraryClient>(() =>
            new SpotifyLibraryClient(config,
                Api.Value,
                WebsocketState.Value,
                loggerFactory.CreateLogger<SpotifyLibraryClient>(),
                PlaybackClient));
    }

    public IWaveePlayer Player { get; }
    internal HttpClient HttpClient { get; }
    internal Lazy<HttpClient> AuthenticatedHttpClient { get; }
    internal Lazy<ISpotifyWebsocketState> WebsocketState { get; }
    internal Lazy<ApResolver> ApResolver { get; }
    internal Lazy<IAudioKeyManager> AudioKeyManager { get; }
    internal ITimeProvider TimeProvider { get; }
    public Lazy<ISpotifyApiClient> Api { get; }
    public Lazy<ISpotifyTokenClient> Token { get; }
    public ISpotifyPlaybackClient Playback { get; }
    public Lazy<ISpotifyLibraryClient> Library { get; }
    public Lazy<ISpotifyPlaylistClient> Playlist { get; }


    /// <summary>
    /// Creates and configures the <see cref="HttpClient"/> used for Spotify API interactions.
    /// </summary>
    /// <param name="tokenClient">
    ///     An optional instance of <see cref="ISpotifyTokenClient"/> to use for authenticating requests.
    /// </param>
    /// <param name="apResolver">
    ///   An optional instance of <see cref="ApResolver"/> to use for resolving Spotify AP endpoints.
    /// </param>
    /// <returns>A configured instance of <see cref="HttpClient"/>.</returns>
    private static HttpClient CreateHttpClient(
        SpotifyConfig config,
        ILoggerFactory loggerFactory,
        ISpotifyTokenClient? tokenClient = null, ApResolver? apResolver = null)
    {
        var retryPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new HttpRetryStrategyOptions
            {
                BackoffType = DelayBackoffType.Exponential,
                MaxRetryAttempts = 3
            })
            .Build();

        var socketHandlerx = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
            AutomaticDecompression = DecompressionMethods.All
        };

        var loggingHandler = new HttpLoggingHandler(loggerFactory.CreateLogger<HttpLoggingHandler>())
        {
            InnerHandler = socketHandlerx
        };

#pragma warning disable EXTEXP0001
        DelegatingHandler resilienceHandler = new ResilienceHandler(retryPipeline)
#pragma warning restore EXTEXP0001
        {
            InnerHandler = loggingHandler,
        };
        if (apResolver != null)
        {
            resilienceHandler = new ApResolvingHandler(apResolver)
            {
                InnerHandler = resilienceHandler
            };
        }

        if (tokenClient != null)
        {
            resilienceHandler = new AuthenticatedHttpClientHandler(tokenClient)
            {
                InnerHandler = resilienceHandler
            };
            // // Now add a caching handler
            // var cacheRepository = new SqliteCacheRepository<string>(config.Cache.Location,
            //     loggerFactory.CreateLogger<SqliteCacheRepository<string>>());
            // var cacheKeyBuilder = new DefaultCacheKeyBuilder();
            // resilienceHandler = new CachingHandler(cacheRepository, cacheKeyBuilder,
            //     loggerFactory.CreateLogger<CachingHandler>())
            // {
            //     InnerHandler = resilienceHandler
            // };
        }

        var errorsAreAllWaveeExceptionsHandler =
            new WaveeExceptionHandler(loggerFactory.CreateLogger<WaveeExceptionHandler>())
            {
                InnerHandler = resilienceHandler
            };
        return new HttpClient(errorsAreAllWaveeExceptionsHandler);
    }

    /// <summary>
    /// Gets the JSON serializer options used for serializing and deserializing Spotify API responses.
    /// </summary>
    public static JsonSerializerOptions DefaultJsonOptions { get; } = new JsonSerializerOptions
    {
        TypeInfoResolver = AppJsonSerializerContext.Default
    };

    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(ApResolver.ApResolveData))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(string[]))]
    [JsonSerializable(typeof(bool))]
    [JsonSerializable(typeof(SendPing))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(SendRequestReply))]
    [JsonSerializable(typeof(SendRequestReply.SendRequestReplyPayload))]
    [JsonSerializable(typeof(List<SpotifyCachedPlaylistTrack>))]
    [JsonSerializable(typeof(SpotifyCachedPlaylistTrack))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {
    }

    /// <inheritdoc/>
    public ISpotifyTokenClient TokenClient => Token.Value;

    /// <inheritdoc/>
    public ISpotifyPlaybackClient PlaybackClient => Playback;

    /// <inheritdoc/>
    public ISpotifyApiClient ApiClient => Api.Value;

    public ISpotifyLibraryClient LibraryClient => Library.Value;
    public ISpotifyPlaylistClient PlaylistClient => Playlist.Value;

    public ValueTask<string> UserId()
    {
        return _sessionHolder.Value.UserId();
    }
}