using Eum.Spotify;
using LanguageExt.UnsafeValueAccess;
using Wavee.Player;
using Wavee.Spotify.Infrastructure.ApResolve;
using Wavee.Spotify.Infrastructure.AudioKey;
using Wavee.Spotify.Infrastructure.Cache;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Spotify.Infrastructure.Mercury;
using Wavee.Spotify.Infrastructure.Playback;
using Wavee.Spotify.Infrastructure.Playback.Contracts;
using Wavee.Spotify.Infrastructure.PrivateApi;
using Wavee.Spotify.Infrastructure.PublicApi;
using Wavee.Spotify.Infrastructure.Remote;
using Wavee.Spotify.Infrastructure.Remote.Contracts;

namespace Wavee.Spotify;

/// <summary>
/// The SpotifyClient class is the main class for interacting with Spotify.
/// </summary>
public sealed class SpotifyClient : IDisposable
{
    private readonly string _deviceId;
    private readonly SpotifyTcpConnection _connection;
    private readonly SpotifyCacheConfig _config;

    //Some clients should be reference types, some should be value types (single-use)
    private SpotifyClient(
        SpotifyConfig config,
        SpotifyTcpConnection connection,
        string deviceId)
    {
        _config = config.Cache;
        _connection = connection;
        _deviceId = deviceId;
        Remote = new SpotifyRemoteClient(
            tokenFactory: (ct) => Mercury.GetAccessToken(ct),
            playbackEvent: (ev) => (Playback as SpotifyPlaybackClient)!.OnPlaybackEvent(ev),
            config: config.Remote,
            deviceId: deviceId,
            userId: connection.LastWelcomeMessage.Value.CanonicalUsername);

        Playback = new SpotifyPlaybackClient(
            mercuryFactory: () => Mercury,
            audioKeyProviderFactory: () => AudioKeyProvder,
            cacheFactory: () => Cache,
            remoteUpdates:
            (state) => (Remote as SpotifyRemoteClient)!.OnPlaybackUpdate(state),
            config:
            config.Playback,
            deviceId:
            deviceId,
            remoteConfig:
            config.Remote,
            countryCode:
            _connection.LastCountryCode,
            ready: (Remote as SpotifyRemoteClient)!.Ready);
    }

    public static async Task<SpotifyClient> CreateAsync(
        SpotifyConfig config,
        LoginCredentials credentials)
    {
        await ApResolver.Populate();

        WaveePlayer.Instance.CrossfadeDuration = config.Playback.CrossfadeDuration;
        
        var firstAp = ApResolver.AccessPoint.ValueUnsafe();
        var split = firstAp.Split(':');

        var host = split[0];
        var port = ushort.Parse(split[1]);

        var deviceId = Guid.NewGuid().ToString();
        var connection = new SpotifyTcpConnection(
            host: host,
            port: port,
            credentials: credentials,
            deviceId: deviceId
        );

        return new SpotifyClient(config, connection, deviceId);
    }

    /// <summary>
    /// The specific Spotify remote state.
    /// </summary>
    public ISpotifyRemoteClient Remote { get; }

    /// <summary>
    /// A client for performing Spotify playback actions.
    /// </summary>
    public ISpotifyPlaybackClient Playback { get; }

    /// <summary>
    /// A client for performing actions on the Spotify Web API.
    /// The one that is already available to the public.
    /// </summary>
    public ISpotifyPublicApi PublicApi => new SpotifyPublicApi(
        tokenFactory: (ct) => Mercury.GetAccessToken(ct));

    /// <summary>
    /// A client for performing actions on a private Spotify Web API.
    /// These clients utilise a region-based url endpoint,
    /// and all these actions can also be performed by using the Mercury client. 
    /// </summary>
    public ISpotifyPrivateApi PrivateApi => new SpotifyPrivateApi(
        tokenFactory: (ct) => Mercury.GetAccessToken(ct));

    /// <summary>
    /// A client for performing actions on the Spotify Mercury API (hm://)
    /// </summary>
    public ISpotifyMercuryClient Mercury => new MercuryClient(
        username: _connection.LastWelcomeMessage.Value.CanonicalUsername,
        countryCode: _connection.LastCountryCode.Value.IfNone("US"),
        onPackageSend: (pkg) => _connection.Send(pkg),
        onPackageReceive: (condition) => _connection.CreateListener(condition)
    );


    public ISpotifyCache Cache => new SpotifyCache(
        root: _config.CacheRoot
    );

    public IAudioKeyProvider AudioKeyProvder => new AudioKeyProvider(
        username: _connection.LastWelcomeMessage.Value.CanonicalUsername,
        onPackageSend: (pkg) => _connection.Send(pkg),
        onPackageReceive: (condition) => _connection.CreateListener(condition));

    public void Dispose()
    {
        _connection.Dispose();

        if (Remote is IDisposable remote)
            remote.Dispose();

        if (Playback is IDisposable playback)
            playback.Dispose();
    }
}