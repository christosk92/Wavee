using System.ComponentModel;
using System.Net.WebSockets;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using AsyncKeyedLock;
using Eum.Spotify.connectstate;
using ReactiveUI;
using Wavee.Core;
using Wavee.Spotify.Authenticators;
using Wavee.Spotify.Http.Interfaces;
using Wavee.Spotify.Http.Interfaces.Clients;
using Wavee.Spotify.Models.Response;

namespace Wavee.Spotify.Http.Clients;

public class PlayerClient : ApiClient, IPlayerClient, INotifyPropertyChanged
{
    private readonly AsyncNonKeyedLocker _connectionLocker = new();
    private readonly string _deviceId;

    private readonly IAuthenticator _authenticator;
    private readonly IObservable<Cluster> _clusterChanged;

    private SpotifyPrivateDevice? _device;
    private SpotifyWebsocketConnection? _connection;
    private IObservable<WaveePlaybackState> _localPlaybackStateChanged;
    private readonly ISpotifyClient _parentClient;

    public PlayerClient(
        ISpotifyClient parentClient,
        IWaveePlayer player,
        IAPIConnector apiConnector,
        string deviceId,
        IAuthenticator authenticator) : base(apiConnector)
    {
        _deviceId = deviceId;
        _authenticator = authenticator;
        _parentClient = parentClient;
        _device = null;
        _localPlaybackStateChanged = player.Events;
        
        _clusterChanged = this.WhenAnyValue(x => x.Connection)
            .SelectMany(y =>
            {
                if (y is null) return Observable.Empty<Cluster>();
                return y.ClusterChanged.StartWith(y._cluster ?? new Cluster());
            });
    }

    private SpotifyWebsocketConnection? Connection
    {
        get => _connection;
        set => SetField(ref _connection, value);
    }

    public ValueTask<SpotifyPrivateDevice> Connect(string deviceName, DeviceType deviceType,
        CancellationToken cancel = default)
    {
        using var locker = _connectionLocker.Lock(cancel);
        if (_device != null)
        {
            _device.DeviceName = deviceName;
            _device.DeviceType = deviceType;
            return new ValueTask<SpotifyPrivateDevice>(_device);
        }

        return new ValueTask<SpotifyPrivateDevice>(ConnectAsync(deviceName, deviceType, cancel));
    }

    public Task<SpotifyCurrentlyPlaying> GetCurrentlyPlaying(CancellationToken cancel = default)
    {
        throw new NotImplementedException();
    }

    private async Task<SpotifyPrivateDevice> ConnectAsync(string deviceName, DeviceType deviceType,
        CancellationToken cancel)
    {
        var connection = await CreateConnection(deviceName, deviceType, true, cancel);
        _connection = connection;

        return new SpotifyPrivateDevice(
            parentClient: _parentClient,
            deviceId: _deviceId,
            deviceName: deviceName,
            deviceType: deviceType,
            clusterChanged: _clusterChanged,
            localPlaybackStateChanged: _localPlaybackStateChanged,
            updateState: UpdateState,
            async () =>
            {
                if (Connection is not null)
                {
                    await Connection.Close(WebSocketCloseStatus.NormalClosure, "Manually closed");
                }
            }
        );
    }

    private Task UpdateState(string name, DeviceType type, PutStateReason reason, PlayerState state,
        CancellationToken cancellationToken)
    {
        if (Connection == null)
        {
            return Task.CompletedTask;
        }

        return Connection.UpdateState(_deviceId, name, type, reason, state, cancellationToken);
    }

    private async Task<SpotifyWebsocketConnection> CreateConnection(
        string deviceName,
        DeviceType deviceType,
        bool forceCreateNew, CancellationToken cancel)
    {
        using var locker = await _connectionLocker.LockAsync(cancel);
        if (Connection != null)
        {
            if (!forceCreateNew)
            {
                return Connection;
            }

            await Connection.Close(WebSocketCloseStatus.NormalClosure, "Reconnecting");
            Connection.Dispose();
            Connection = null;
        }


        ClientWebSocket clientWebSocket = new();
        clientWebSocket.Options.KeepAliveInterval = TimeSpan.FromHours(1);
        clientWebSocket.Options.SetRequestHeader("Origin", "https://open.spotify.com");
        const string url = "wss://gae2-dealer.spotify.com?access_token=";
        var token = await _authenticator.GetToken(_deviceId, Api, cancel);
        var uri = new Uri(url + token);
        await clientWebSocket.ConnectAsync(uri, cancel);

        var compositeDisposable = new CompositeDisposable();
        var connection = new SpotifyWebsocketConnection(clientWebSocket, Api);
        compositeDisposable.Add(connection);

        connection.OnDisconnected
            .SelectMany(async x =>
            {
                await OnDisconnected(connection, x.Item1, x.Item2, CancellationToken.None);
                return Unit.Default;
            })
            .Subscribe()
            .DisposeWith(compositeDisposable);

        await connection.UpdateState(_deviceId, deviceName, deviceType, PutStateReason.NewDevice, null, cancel);
        return connection;
    }

    private ValueTask OnDisconnected(SpotifyWebsocketConnection connection,
        Exception? error,
        WebSocketCloseStatus? socketCloseStatus,
        CancellationToken cancellationToken)
    {
        if (Connection == connection)
        {
            Connection = null;
        }

        if (socketCloseStatus == WebSocketCloseStatus.NormalClosure)
        {
            // do not reconnect
            return ValueTask.CompletedTask;
        }

        var latestName = _device?.DeviceName ?? "Unknown";
        var latestType = _device?.DeviceType ?? DeviceType.Unknown;
        return new ValueTask(CreateConnection(latestName,
            latestType,
            true, cancellationToken));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}