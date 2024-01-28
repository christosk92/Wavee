using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks.Sources;
using Eum.Spotify;
using Eum.Spotify.connectstate;
using Eum.Spotify.context;
using Eum.Spotify.login5v3;
using Eum.Spotify.storage;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Wavee.Spfy.DefaultServices;
using Wavee.Spfy.Playback;
using Wavee.Spfy.Playback.Decrypt;
using Wavee.Spfy.Remote;

namespace Wavee.Spfy;

internal readonly record struct SendSpotifyPacket(SpotifyPacketType Type, ReadOnlyMemory<byte> Payload);

public sealed class WaveeSpotifyClient
{
    private bool _manualClose;
    private readonly Services _services;
    private readonly Guid _instanceId;
    private readonly ILogger _logger;
    private TaskCompletionSource<string>? _countryCodeTask;
    private readonly Func<ValueTask<string>> _countryCodeTaskFactory;
    internal readonly string _deviceId;
    private TcpConnectionStatusType __tcpConnectionStatus;
    (string ForUser, string Token, DateTimeOffset Expiration)? token = null;
    private readonly List<Channel<SendSpotifyPacket>> _waitingForPackages = new();

    private TcpConnectionStatusType _tcpConnectionStatus
    {
        get => __tcpConnectionStatus;
        set
        {
            if (__tcpConnectionStatus == value)
                return;

            __tcpConnectionStatus = value;
            TcpConnectionStatusChanged?.Invoke(this, value);
        }
    }

    private readonly Channel<SendSpotifyPacket> _sendChannel = Channel.CreateUnbounded<SendSpotifyPacket>();

    public WaveeSpotifyClient(OpenBrowser openBrowser,
        ISecureStorage secureStorage,
        ILogger logger,
        WaveePlayer waveePlayer)
    {
        _deviceId = Guid.NewGuid().ToString("N");

        _logger = logger;
        WaveePlayer = waveePlayer;
        _instanceId = Guid.NewGuid();

        var services = new Services(
            new DefaultHttpClient(),
            new DefaultGzipHttpClient(),
            () => new DefaultTcpClient(_instanceId),
            () => new DefaultWebsocketClient(),
            openBrowser,
            secureStorage
        );

        _services = services;

        _countryCodeTaskFactory = () =>
        {
            if (_countryCodeTask is null)
            {
                _countryCodeTask = new TaskCompletionSource<string>();
                return new ValueTask<string>(_countryCodeTask.Task);
            }

            if (_countryCodeTask.Task.IsCompletedSuccessfully)
            {
                return new ValueTask<string>(_countryCodeTask.Task.Result);
            }

            return new ValueTask<string>(_countryCodeTask.Task);
        };

        Remote = new WaveeSpotifyRemoteClient(
            mainConnectionInstanceId: _instanceId,
            websocketClientFactory: services.WebsocketClientFactory,
            gzipHttpClient: services.GzipHttpClient,
            httpClient: services.HttpClient,
            tokenFactory: TokenFactory,
            deviceId: _deviceId,
            player: waveePlayer,
            logger: logger
        );

        Metadata = new WaveeSpotifyMetadataClient(
            gzipHttpClient: services.GzipHttpClient,
            httpClient: services.HttpClient,
            tokenFactory: TokenFactory,
            logger: logger,
            Option<ICachingProvider>.None
        );
        Library = new WaveeSpotifyLibraryClient(
            httpClient: services.HttpClient,
            tokenFactory: TokenFactoryWithName,
            Option<ICachingProvider>.None
        );

        Playback = new WaveeSpotifyPlaybackClient(
            httpClient: services.HttpClient,
            tokenFactory: TokenFactory,
            audiokeyFactory: AudioKeyFactory,
            logger: logger,
            Option<ICachingProvider>.None,
            waveePlayer,
            Metadata,
            _instanceId
        );

        EntityManager.SetRootClient(_instanceId, this);
        return;
    }

    public readonly WaveePlayer WaveePlayer;
    public readonly WaveeSpotifyRemoteClient Remote;
    public readonly WaveeSpotifyMetadataClient Metadata;
    public readonly WaveeSpotifyPlaybackClient Playback;
    public readonly WaveeSpotifyLibraryClient Library;

    public event EventHandler<TcpConnectionStatusType> TcpConnectionStatusChanged;
    public ValueTask<string> GetAccessToken() => TokenFactory();
    public Exception LastError { get; private set; }
    public ValueTask<string> CountryCode => _countryCodeTaskFactory();
    public Guid InstanceId => _instanceId;

    /// <summary>
    /// Authenticates the client with the specified username.
    ///
    /// This will hit the SecureStorage to see if there are any stored credentials for the specified username.
    ///
    /// If there are no stored credentials, or the authentication fails, the client will open a browser to the Spotify login page.
    /// </summary>
    /// <param name="username">
    /// The username to authenticate with.
    /// </param>
    /// <returns>
    /// A value task that may be completed if there was already an established session for the specified username.
    /// Or else, the value task will complete when the authentication process is complete.
    /// </returns>
    public ValueTask Authenticate(string username)
    {
        if (_services.SecureStorage.TryGetStoredCredentialsForUser(username, out var password))
        {
            var split = password.Split(';');
            var authData = ByteString.FromBase64(split[0]);
            var type = (AuthenticationType)int.Parse(split[1]);
            var credentials = new LoginCredentials
            {
                Username = username,
                AuthData = authData,
                Typ = type
            };

            return Authenticate(credentials);
        }

        var chainedTask = ChainedAuthenticationTask();
        return new ValueTask(chainedTask);
    }

    /// <summary>
    /// Authenticates the client with the default user.
    ///
    /// This will hit the SecureStorage to see if there are any default saved credentials.
    ///
    /// If there are no stored credentials, or the authentication fails, the client will open a browser to the Spotify login page.
    /// </summary>
    /// <returns>
    /// A value task that may be completed if there was already an established session for the specified username.
    /// Or else, the value task will complete when the authentication process is complete.
    /// </returns>
    public ValueTask Authenticate()
    {
        if (_services.SecureStorage.TryGetDefaultUser(out var userId))
        {
            return Authenticate(userId);
        }

        var chainedTask = ChainedAuthenticationTask();

        return new ValueTask(chainedTask);
    }
    public Task<Task> AuthenticateButFailImmediatlyIfOAuthRequired()
    {
        if (_services.SecureStorage.TryGetDefaultUser(out var userId))
        {
            return Task.FromResult(Task.Run(async () => await Authenticate(userId)));
        }

        return Task.FromResult(Task.FromException(new Exception("Requires OAuth")));
        // return ValueTask.FromException(new Exception("No stored credentials found"));
    }

    private async Task ChainedAuthenticationTask()
    {
        var browser = await OAuth.AuthenticateWithBrowser(_services.OpenBrowser, _services.HttpClient);
        await AuthenticateWithCredentialsAsyncOrRedoWithOAuthIfFail(browser);
    }


    /// <summary>
    /// Authenticates the client with the specified credentials directly.
    /// </summary>
    /// <param name="credentials">
    /// The credentials to authenticate with.
    /// </param>
    /// <returns>
    /// A value task that may be completed if there was already an established session for the specified username.
    /// Or else, the value task will complete when the authentication process is complete.
    /// </returns>
    public ValueTask Authenticate(LoginCredentials credentials)
    {
        if (EntityManager.TryGetConnection(_instanceId, out var conn, out var welcomeMessage))
        {
            if (welcomeMessage!.CanonicalUsername == credentials.Username)
            {
                return ValueTask.CompletedTask;
            }

            conn!.Dispose();
        }

        return new ValueTask(AuthenticateWithCredentialsAsyncOrRedoWithOAuthIfFail(credentials));
    }

    private async Task AuthenticateWithCredentialsAsyncOrRedoWithOAuthIfFail(LoginCredentials credentials)
    {
        try
        {
            credentials = await AuthenticateAsync(credentials);

            var countryCodeTaskTcs = new TaskCompletionSource<string>();
            var cts = new CancellationTokenSource();
            new Thread(async () =>
            {
                try
                {
                    Stopwatch pingTimer = new();
                    while (true)
                    {
                        if (!EntityManager.TryGetConnection(_instanceId, out var tcp, out var _))
                        {
                            var err = new Exception("Connection was closed unexpectedly.");
                            LastError = err;
                            _tcpConnectionStatus = TcpConnectionStatusType.NotConnectedDueToError;
                            throw err;
                        }

                        void DoLoop()
                        {
                            var type = tcp.Receive(out var payload);
                            switch (type)
                            {
                                case SpotifyPacketType.CountryCode:
                                    countryCodeTaskTcs.TrySetResult(Encoding.UTF8.GetString(payload));
                                    break;
                                case SpotifyPacketType.Ping:
                                    //send pong
                                    _logger.LogDebug("Received ping. Sending pong");
                                    pingTimer = Stopwatch.StartNew();
                                    tcp.Send(SpotifyPacketType.Pong, new byte[4]);
                                    break;
                                case SpotifyPacketType.PongAck:
                                    {
                                        var ping = pingTimer.Elapsed;
                                        _logger.LogInformation("Ping: {Ping}", ping / 2);
                                        pingTimer.Stop();
                                        break;
                                    }
                                default:
                                    {
                                        foreach (var channel in _waitingForPackages.ToList())
                                        {
                                            if (!channel.Writer.TryWrite(new SendSpotifyPacket(type, payload.ToArray())))
                                            {
                                                _waitingForPackages.Remove(channel);
                                            }
                                        }

                                        _logger.LogDebug("Received unhandled packet type: {PacketType}", type);
                                        break;
                                    }
                            }
                        }

                        DoLoop();
                    }
                }
                catch (Exception e)
                {
                    cts.Cancel();
                    _logger.LogError(e, "Spotify connection failed unexpectedly.");
                    LastError = e;
                    _tcpConnectionStatus = TcpConnectionStatusType.NotConnectedDueToError;
                }

                if (!_manualClose)
                {
                    _logger.LogInformation("Connection closed. Reconnecting...");
                    EntityManager.RemoveConnection(_instanceId);
                    _countryCodeTask = null;
                    await AuthenticateWithCredentialsAsyncOrRedoWithOAuthIfFail(credentials);
                }
                else
                {
                    _tcpConnectionStatus = TcpConnectionStatusType.NotConnected;
                }
            }).Start();

            new Thread(async () =>
            {
                try
                {
                    while (!cts.IsCancellationRequested)
                    {
                        var canRead = await _sendChannel.Reader.WaitToReadAsync(cts.Token);
                        if (!canRead)
                        {
                            break;
                        }

                        var msg = await _sendChannel.Reader.ReadAsync(cts.Token);
                        if (!EntityManager.TryGetConnection(_instanceId, out var tcp, out var _))
                        {
                            // Put the message back in the channel
                            await _sendChannel.Writer.WriteAsync(msg, cts.Token);
                            break;
                        }

                        try
                        {
                            tcp?.Send(msg.Type, msg.Payload.Span);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, "Failed to send message(Type={MessageType}) to Spotify", msg.Type);
                            // Put the message back in the channel
                            await _sendChannel.Writer.WriteAsync(msg, cts.Token);
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to send message to Spotify");
                }
                finally
                {
                    cts.Dispose();
                }
            }).Start();

            var remoteConnectionTask = Remote.Connect("Wavee", DeviceType.Computer).AsTask();
            var countryCodeTask = countryCodeTaskTcs.Task;
            await Task.WhenAll(remoteConnectionTask, countryCodeTask);
            _countryCodeTask ??= new TaskCompletionSource<string>();

            var countryCode = await countryCodeTask;
            _countryCodeTask.TrySetResult(countryCode);
            EntityManager.SetCountryCode(_instanceId, countryCode);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to authenticate with credentials. Falling back to OAuth");

            await _services.SecureStorage.Remove(credentials.Username);

            var browser = await OAuth.AuthenticateWithBrowser(_services.OpenBrowser, _services.HttpClient);
            await Authenticate(browser);
        }
    }

    private async Task<LoginCredentials> AuthenticateAsync(LoginCredentials credentials)
    {
        var tcpClient = _services.TcpClientFactory();

        var httpClient = _services.HttpClient;
        _tcpConnectionStatus = TcpConnectionStatusType.Connecting;

        var (ap, port) = await ApResolve.GetAccessPoint(httpClient);

        await tcpClient.Connect(ap, port);
        tcpClient.Handshake();
        tcpClient.Authenticate(credentials, _deviceId);
        if (!tcpClient.IsConnected)
        {
            var err = new Exception("Failed to authenticate with Spotify");
            LastError = err;
            _tcpConnectionStatus = TcpConnectionStatusType.NotConnectedDueToError;
            throw err;
        }

        if (EntityManager.TryGetConnection(_instanceId, out var conn, out var welcomeMessage))
        {
            if (welcomeMessage!.CanonicalUsername == credentials.Username)
            {
                _logger.LogInformation("Successfully authenticated with Spotify");

                await _services.SecureStorage.Store(welcomeMessage.CanonicalUsername,
                    $"{welcomeMessage.ReusableAuthCredentials.ToBase64()};{(int)welcomeMessage.ReusableAuthCredentialsType}");
                _tcpConnectionStatus = TcpConnectionStatusType.Connected;
                return new LoginCredentials
                {
                    AuthData = welcomeMessage.ReusableAuthCredentials,
                    Username = welcomeMessage.CanonicalUsername,
                    Typ = welcomeMessage.ReusableAuthCredentialsType
                };
            }
        }

        throw new Exception("Failed to authenticate with Spotify");
    }

    internal Channel<SendSpotifyPacket> Send(SendSpotifyPacket package)
    {
        var channel = Channel.CreateUnbounded<SendSpotifyPacket>();
        _waitingForPackages.Add(channel);

        _sendChannel.Writer.TryWrite(package);
        return channel;
    }

    ValueTask<string> TokenFactory()
    {
        if (token is not null && token.Value.Expiration > DateTimeOffset.UtcNow)
        {
            return new ValueTask<string>(token.Value.Token);
        }

        var linkedTask = Tokens.GetToken(_instanceId, _services.HttpClient, _deviceId).ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                throw t.Exception!.InnerException!;
            }

            var x = (t.Result.Username, t.Result.AccessToken, t.Result.Expires);
            token = x;
            return x.AccessToken;
        });

        return new ValueTask<string>(linkedTask);
    }
    ValueTask<(string, string)> TokenFactoryWithName()
    {
        if (token is not null && token.Value.Expiration > DateTimeOffset.UtcNow)
        {
            return new ValueTask<(string, string)>((token.Value.ForUser, token.Value.Token));
        }

        var linkedTask = Tokens.GetToken(_instanceId, _services.HttpClient, _deviceId).ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                throw t.Exception!.InnerException!;
            }

            var x = (t.Result.Username, t.Result.AccessToken, t.Result.Expires);
            token = x;
            return (x.Username, x.AccessToken);
        });

        return new ValueTask<(string, string)>(linkedTask);
    }


    private static Dictionary<ByteString, Option<byte[]>> _audioKeyCache = new();

    ValueTask<Option<byte[]>> AudioKeyFactory(SpotifyId id, ByteString fileId)
    {
        if (_audioKeyCache.TryGetValue(fileId, out var cached))
        {
            return new ValueTask<Option<byte[]>>(cached);
        }

        var linkedTask = AudioKey.GetAudioKey(_instanceId, id, fileId).ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                throw t.Exception!.InnerException!;
            }

            var x = t.Result;
            _audioKeyCache[fileId] = x;
            return x;
        });

        return new ValueTask<Option<byte[]>>(linkedTask);
    }
}

/// <summary>
/// A callback delegate that requests a browser open event for the specified url.
///
/// The function should return when a route is made to a url that matches the format.
/// </summary>
public delegate ValueTask<string> OpenBrowser(string url, Func<string, bool> shouldReturn);

internal interface IHttpClient
{
    Task<SpotifyTokenResult> SendLoginRequest(Dictionary<string, string> body, CancellationToken none);
    Task<(string ap, string dealer, string sp)> FetchBestAccessPoints();
    Task<LoginResponse> SendLoginStepTwoRequest(LoginCredentials credentials, string deviceId, CancellationToken none);
    Task<HttpResponseMessage> Get(string endpointWithId, string accessToken, CancellationToken cancellationToken);

    Task<HttpResponseMessage> Get(string url);

    Task<StorageResolveResponse> StorageResolve(string fileFileIdBase16, string accessToken,
        CancellationToken cancellationToken = default);

    Task<SpotifyEncryptedStream> CreateEncryptedStream(string cdnUrl, CancellationToken cancellationToken = default);
    Task<Context> ResolveContext(string itemId, string accessToken);
    Task<ContextPage> ResolveContextRaw(string pageUrl, string accessToken);
    Task<HttpResponseMessage> Post(string url, string token, byte[] content,
        string contentType);

    Task<HttpResponseMessage> GetGraphQL(string token, string operationName, string operationHash, Dictionary<string, object> variables);
    Task<HttpResponseMessage> SendCommand(string token, object command, string fromDeviceId, string activeDeviceId);
    Task<HttpResponseMessage> SendVolumeCommand(string token, object command, string fromDeviceId, string activeDeviceId);

    Task<HttpResponseMessage> Send(HttpRequestMessage request, CancellationToken none);
}

internal interface IGzipHttpClient
{
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);

    Task<Cluster> PutState(IHttpClient httpClient,
        Func<ValueTask<string>> tokenFactory,
        string connectionId,
        PutStateRequest putState,
        CancellationToken cancellationToken = default);
}

internal interface IWebsocketClient : IDisposable
{
    bool IsConnected { get; }
    string ConnectionId { get; }

    Task Connect(string url, CancellationToken cancellationToken);
    Task<SpotifyWebsocketMessage> ReadNextMessage(CancellationToken cancellationToken);
    Task SendPing(CancellationToken ca);
    Task SendJson(string reply, CancellationToken none);
}

public enum TcpConnectionStatusType
{
    NotConnected,
    Connecting,
    Connected,
    NotConnectedDueToError
}