using Eum.Spotify;
using LanguageExt;
using Wavee.Spfy.DefaultServices;

namespace Wavee.Spfy;

internal readonly struct Services
{
    public readonly IHttpClient HttpClient;
    public readonly IGzipHttpClient GzipHttpClient;
    public readonly Func<ITcpClient> TcpClientFactory;
    public readonly Func<IWebsocketClient> WebsocketClientFactory;
    public readonly OpenBrowser OpenBrowser;
    public readonly ISecureStorage SecureStorage;

    public Services(IHttpClient httpClient,
        IGzipHttpClient gzipHttpClient
        , Func<ITcpClient> tcpClientFactory,
        Func<IWebsocketClient> websocketClientFactory,
        OpenBrowser openBrowser,
        ISecureStorage secureStorage)
    {
        HttpClient = httpClient;
        GzipHttpClient = gzipHttpClient;
        TcpClientFactory = tcpClientFactory;
        WebsocketClientFactory = websocketClientFactory;
        OpenBrowser = openBrowser;
        SecureStorage = secureStorage;
    }
}

internal interface ITcpClient : IDisposable
{
    bool IsConnected { get; }
    ValueTask Connect(string host, ushort port);
    void Handshake();
    APWelcome Authenticate(LoginCredentials credentials, string deviceId);
    void Send(SpotifyPacketType packageType, ReadOnlySpan<byte> packagePayload);
    SpotifyPacketType Receive(out ReadOnlySpan<byte> payload);
}

public interface ISecureStorage
{
    ValueTask Remove(string username);
    ValueTask Store(string username, string pwd);
    bool TryGetDefaultUser(out string userId);
    bool TryGetStoredCredentialsForUser(string userId, out string password);
}

public interface ICachingProvider
{
    Unit Set(string cacheKey, byte[] bytes);
    bool TryGet(object cacheKey, out byte[] bytes);
}

internal readonly struct SpotifyWebsocketMessage
{
    public SpotifyWebsocketMessage(SpotifyWebsocketMessageType type, ReadOnlyMemory<byte> payload, string url, string key)
    {
        Type = type;
        Payload = payload;
        Url = url;
        Key = key;
    }

    public readonly string Key;
    public readonly string Url;
    public readonly SpotifyWebsocketMessageType Type;
    public readonly ReadOnlyMemory<byte> Payload;
}

internal enum SpotifyWebsocketMessageType
{
    ConnectionId,
    Message,
    Request,
    Pong
}

public readonly struct SpotifyTokenResult
{
    public readonly string AccessToken;
    public readonly DateTimeOffset Expires;
    public readonly string Username;

    public SpotifyTokenResult(string? accessToken, DateTimeOffset addSeconds, string? finalUsername)
    {
        AccessToken = accessToken ?? throw new ArgumentNullException(nameof(accessToken));
        Expires = addSeconds;
        Username = finalUsername ?? throw new ArgumentNullException(nameof(finalUsername));
    }
}