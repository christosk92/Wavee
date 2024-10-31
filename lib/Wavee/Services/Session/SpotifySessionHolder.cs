using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using Eum.Spotify;
using Microsoft.Extensions.Logging;
using NeoSmart.AsyncLock;
using Wavee.Config;
using Wavee.Enums;
using Wavee.Exceptions;
using Wavee.Interfaces;
using AsyncManualResetEvent = Nito.AsyncEx.AsyncManualResetEvent;

namespace Wavee.Services.Session;

internal sealed class SpotifySessionHolder : ISpotifySessionHolder, IPacketDispatcher, ICredentialsProvider,
    IDisposable,
    ICountryProvider
{
    private readonly ApResolver _apResolver;
    private readonly ILogger<SpotifySessionHolder> _logger;
    private readonly HttpClient _httpClient;
    private readonly ITcpClientFactory _tcpClientFactory;
    private readonly SpotifyConfig _config;
    private readonly IOAuthClient _oAuthClient;
    private readonly CancellationTokenSource _masterCts = new();
    private readonly AsyncManualResetEvent _connectedEvent = new(false);

    public SpotifySessionHolder(
        ApResolver apResolver,
        SpotifyConfig config,
        HttpClient httpClient,
        IOAuthClient oAuthClient,
        ITcpClientFactory tcpClientFactory,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<SpotifySessionHolder>();
        _apResolver = apResolver;
        _httpClient = httpClient;
        _tcpClientFactory = tcpClientFactory;
        _oAuthClient = oAuthClient;
        _config = config;

        Task.Run(Runner);
    }
    //public string UserId => _finalCredentials?.Username ?? throw new WaveeUnknownException("User ID is not set");

    public void SetAudioKeyDispatcher(IAudioKeyManager audioKeyManager)
    {
        _audioKeyManager = audioKeyManager;
    }

    private async Task Runner()
    {
        bool attemptReconnect = false;
        while (!_masterCts.Token.IsCancellationRequested)
        {
            if (!attemptReconnect)
            {
                await _connectedEvent.WaitAsync(_masterCts.Token);
            }

            try
            {
                await EnsureConnectedAsync(false, _masterCts.Token);
                _connected.OnNext(true);
                // Read message 
                var message = await _tcpClient!.ReadMessageAsync(_masterCts.Token);
                switch (message.Type)
                {
                    case PacketType.Ping:
                        var ping = new byte[4];
                        await _tcpClient.SendPacketAsync(new SpotifyTcpMessage(PacketType.Pong, ping),
                            _masterCts.Token);
                        break;
                    case PacketType.PongAck:
                        _logger.LogInformation("PongAck received");
                        break;
                    case PacketType.CountryCode:
                        var countryCode = Encoding.UTF8.GetString(message.Payload);
                        _countryCode = countryCode;
                        _countryCodeEvent.Set();
                        _logger.LogInformation("Country code set to {CountryCode}", countryCode);
                        break;
                    case PacketType.AesKey:
                    case PacketType.AesKeyError:
                    {
                        if (_audioKeyManager is null)
                        {
                            _logger.LogWarning("Received AES key but no audio key manager is set");
                            break;
                        }

                        _audioKeyManager.Dispatch(message.Type, message.Payload);
                        break;
                    }
                    default:
                        // Log: Received unhanded packet of type {message.Type} and length {message.Payload.Length}
                        _logger.LogWarning("Received unhanded packet of type {Type} and length {Length}",
                            message.Type, message.Payload.Length);
                        break;
                }
            }
            catch (OperationCanceledException x)
            {
                if (x.CancellationToken == _masterCts.Token)
                {
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "An error occurred in the session runner.. We will attempt a reconnect in 5 seconds");
                _tcpClient?.Dispose();
                _tcpClient = null;
                _finalCredentials = null;
                _connectedEvent.Reset();
                _countryCodeEvent.Reset();
                attemptReconnect = true;
                _connected.OnNext(false);
                await Task.Delay(5000, _masterCts.Token);
            }
        }

        _masterCts.Dispose();
    }

    public Task SendPacketAsync(PacketType type, byte[] payload)
    {
        if (_tcpClient is null)
        {
            throw new WaveeNetworkException("Not connected to Spotify");
        }

        return _tcpClient!.SendPacketAsync(new SpotifyTcpMessage(type, payload), _masterCts.Token);
    }

    public string ClientId { get; } = "65b708073fc0480ea92a077233ca87bd";

    public string Scopes { get; } =
        "playlist-modify ugc-image-upload user-follow-read user-read-email user-read-private app-remote-control streaming user-follow-modify user-modify-playback-state user-library-modify playlist-modify-public playlist-read user-read-birthdate user-top-read playlist-read-private playlist-read-collaborative user-modify-private playlist-modify-private user-modify user-library-read user-personalized user-read-play-history user-read-playback-state user-read-currently-playing user-read-recently-played user-read-playback-position";

    public ValueTask<LoginCredentials> GetUserCredentialsAsync(CancellationToken cancellationToken)
    {
        // Step 1: Check if we have stored credentials
        // Step 2:
        //      A       1. If we have stored credentials, use them to create a new TCP connection
        //              2. After we have a TCP connection, we return the credentials
        //              3. We Also store the credentials for future use
        //      B       1. If we don't have stored credentials, we first prompt the user to login using OAUTH
        //              2. After the user logs in, we go back to step 2A
        return EnsureConnectedAsync(true, cancellationToken);
    }

    public ValueTask<LoginCredentials> EnsureConnectedAsync(bool log, CancellationToken cancellationToken = default)
    {
        try
        {
            using (_credentialsLock.Lock(cancellationToken))
            {
                if (_finalCredentials is not null && _tcpClient is { Connected: true })
                {
                    if (log)
                    {
                        _logger.LogDebug("Using existing credentials");
                    }

                    _connected.OnNext(true);
                    return new ValueTask<LoginCredentials>(_finalCredentials);
                }

                return new ValueTask<LoginCredentials>(ConnectAsync(cancellationToken));
            }
        }
        catch (OperationCanceledException op)
        {
            _connected.OnNext(false);
            throw new WaveeOperationCanceledException("The operation was canceled", op);
        }
        catch (Exception ex) when (ex is not WaveeException)
        {
            _connected.OnNext(false);
            _logger.LogError(ex, "Failed to connect to Spotify");
            throw new WaveeUnknownException("Failed to connect to Spotify", ex);
        }
    }

    private async Task<LoginCredentials> ConnectAsync(CancellationToken cancellationToken)
    {
        using (await _credentialsLock.LockAsync(cancellationToken))
        {
            _connected.OnNext(false);
            _logger.LogInformation("Connecting to Spotify");
            _connectedEvent.Reset();

            // 1. Check if we have stored credentials, these are not the same as FinalCredentials
            LoginCredentials? storedCredentials = null;
            if (_config.CredentialsCache.RetrieveCredentials is not null)
            {
                storedCredentials = await _config.CredentialsCache.RetrieveCredentials(cancellationToken);
                if (storedCredentials is not null)
                {
                    _logger.LogDebug("Using stored credentials");
                }
            }

            // 2. If we have stored credentials, use them to create a new TCP connection
            // if we don't have stored credentials, we first prompt the user to login using OAUTH
            if (storedCredentials is null)
            {
                _logger.LogInformation("No stored credentials found, prompting user to login");
                // Prompt the user to login using OAUTH
                storedCredentials = await _oAuthClient.LoginAsync(ClientId, Scopes, cancellationToken);
                if (storedCredentials is null)
                {
                    throw new WaveeCouldNotAuthenticateException(
                        "OAuth credentials were not properly returned. This could be due to a user canceling the login process.");
                }

                _logger.LogInformation("Successfully logged in with OAUTH... Now performing complex shit");
            }

            // Establish a TCP connection now
            // log: Connecting to TCP
            var host = await _apResolver.ResolveAsync("accesspoint", cancellationToken);
            ITcpClient? tcpClient = null;
            try
            {
                _logger.LogDebug("Connecting to TCP");
                tcpClient = await _tcpClientFactory.CreateAsync(host.Host, host.Port, cancellationToken);
                _logger.LogDebug("Connected to TCP. Authenticating now.");
                var credentials =
                    await tcpClient.Initialize(storedCredentials, _config.Playback.DeviceId, cancellationToken);
                _finalCredentials = credentials;
                _logger.LogInformation("Authenticated to TCP.");
                if (_config.CredentialsCache.StoreCredentials is not null)
                {
                    await _config.CredentialsCache.StoreCredentials(credentials, cancellationToken);
                }

                _tcpClient = tcpClient;
                _connectedEvent.Set();
                _connected.OnNext(true);
                return credentials;
            }
            catch (Exception)
            {
                tcpClient?.Dispose();
                _connectedEvent.Reset();
                throw;
            }
        }
    }

    private ITcpClient? _tcpClient;
    private LoginCredentials? _finalCredentials;
    private AsyncLock _credentialsLock = new();
    private AsyncManualResetEvent _countryCodeEvent = new(false);
    private string? _countryCode;
    private IAudioKeyManager? _audioKeyManager;
    private readonly BehaviorSubject<bool> _connected = new(false);

    public void Dispose()
    {
        _masterCts.Cancel();
        _tcpClient?.Dispose();
    }

    public ValueTask<string> GetCountryCode(CancellationToken cancellationToken = default)
    {
        if (_countryCode is not null)
        {
            return new ValueTask<string>(_countryCode);
        }

        return new ValueTask<string>(GetCountryCodeAsync(cancellationToken));
    }

    public ValueTask<string> UserId(CancellationToken cancellationToken = default)
    {
        if (_finalCredentials is not null)
        {
            return new ValueTask<string>(_finalCredentials.Username);
        }

        return new ValueTask<string>(GetUserIdAsync(cancellationToken));
    }

    private async Task<string> GetCountryCodeAsync(CancellationToken cancellationToken)
    {
        await _countryCodeEvent.WaitAsync(cancellationToken);
        return _countryCode ?? throw new WaveeUnknownException("Country code was not set");
    }

    private async Task<string> GetUserIdAsync(CancellationToken cancellationToken)
    {
        await Task.Run(async () => await EnsureConnectedAsync(true, cancellationToken), cancellationToken);
        return _finalCredentials!.Username;
    }

    public IObservable<bool> Connected => _connected
        .StartWith(_connectedEvent.IsSet)
        .DistinctUntilChanged();
}