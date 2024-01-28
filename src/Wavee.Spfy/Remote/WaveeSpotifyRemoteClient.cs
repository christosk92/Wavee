using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Eum.Spotify.connectstate;
using Eum.Spotify.context;
using Eum.Spotify.transfer;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Logging;
using Serilog;
using TagLib.Matroska;
using Wavee.Spfy.Items;
using Wavee.Spfy.Mapping;
using Wavee.Spfy.Playback;
using Wavee.Spfy.Playback.Contexts;
using static LanguageExt.Prelude;
using Context = Wavee.Spfy.Items.Context;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Restrictions = Eum.Spotify.connectstate.Restrictions;

namespace Wavee.Spfy.Remote;

public sealed class WaveeSpotifyRemoteClient
{
    private const uint VOLUME_STEPS = 12;
    public const uint MAX_VOLUME = 65535;

    private SemaphoreSlim _semaphore = new(1, 1);
    private readonly Guid _instanceId;
    private readonly Func<IWebsocketClient> _websocketClientFactory;
    private readonly IHttpClient _httpClient;
    private readonly IGzipHttpClient _gzipHttpClient;
    private readonly Func<ValueTask<string>> _tokenFactory;
    private readonly string _deviceId;
    private readonly ILogger _logger;
    private readonly Ref<string> _deviceName = Ref("Wavee");
    private readonly Ref<DeviceType> _deviceType = Ref(DeviceType.Computer);

    private bool _manualClose;
    private RemoteConnectionStatusType _status;
    private TaskCompletionSource _pongTcs = new();
    private Option<SpotifyRemoteState> _state;
    private readonly WaveePlayer _player;
    private Option<uint> _lastCommandId = None;
    private Option<string> _lastCommandSentBy = None;

    internal WaveeSpotifyRemoteClient(Guid mainConnectionInstanceId,
        Func<IWebsocketClient> websocketClientFactory,
        IGzipHttpClient gzipHttpClient,
        IHttpClient httpClient,
        Func<ValueTask<string>> tokenFactory, string deviceId,
        WaveePlayer player,
        ILogger logger)
    {
        _instanceId = mainConnectionInstanceId;
        _websocketClientFactory = websocketClientFactory;
        _gzipHttpClient = gzipHttpClient;
        _httpClient = httpClient;
        _tokenFactory = tokenFactory;
        _deviceId = deviceId;
        _player = player;
        _logger = logger;

        // Ping
        new Thread(async () =>
        {
            var tenMilliseconds = TimeSpan.FromMilliseconds(10);
            var fiveSeconds = TimeSpan.FromSeconds(5);
            var halfAMinute = TimeSpan.FromMinutes(0.5);
            while (true)
            {
                if (!EntityManager.TryGetWebsocketConnection(_instanceId, out var ws))
                {
                    await Task.Delay(tenMilliseconds, CancellationToken.None);
                    continue;
                }

                _pongTcs = new TaskCompletionSource();
                _logger.LogDebug("Sending Spotify WebSocket ping...");
                var sw = Stopwatch.StartNew();
                await ws.SendPing(CancellationToken.None);
                await Task.WhenAny(_pongTcs.Task, Task.Delay(fiveSeconds, CancellationToken.None));
                if (!_pongTcs.Task.IsCompleted)
                {
                    _logger.LogWarning("Spotify WebSocket ping timed out. Canceling connection...");
                    EntityManager.RemoveWebsocketConnection(_instanceId);
                }
                else
                {
                    _logger.LogDebug("Spotify WebSocket ping took {Elapsed} ({ElapsedMilliseconds})", sw.Elapsed,
                        sw.ElapsedMilliseconds);
                    await Task.Delay(halfAMinute, CancellationToken.None);
                }
            }
        }).Start();

        var state = new PlayerState();
        Option<Guid> playbackId = Option<Guid>.None;
        Option<Guid> sessionId = Option<Guid>.None;
        Option<DateTimeOffset> startedPlayingAt = Option<DateTimeOffset>.None;


        this.ClusterChanged += async (sender, update) =>
        {
            if (startedPlayingAt.IsNone)
                return;
            var startedPlayingAtValue = startedPlayingAt.ValueUnsafe().ToUnixTimeMilliseconds();
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long ts = update.Cluster.Timestamp - 3000; // Workaround
            if (!_deviceId.Equals(update.Cluster.ActiveDeviceId) && now > startedPlayingAtValue &&
                ts > startedPlayingAtValue)
            {
                playbackId = Option<Guid>.None;
                sessionId = Option<Guid>.None;
                startedPlayingAt = Option<DateTimeOffset>.None;

                state = new PlayerState()
                {
                    PlaybackSpeed = 1,
                    SessionId = string.Empty,
                    PlaybackId = string.Empty,
                    Suppressions = new Suppressions(),
                    ContextRestrictions = new Restrictions(),
                    Options = new ContextPlayerOptions
                    {
                        RepeatingContext = false,
                        RepeatingTrack = false,
                        ShufflingContext = false
                    },
                    PositionAsOfTimestamp = 0,
                    Position = 0,
                    IsPlaying = false
                };
                _lastCommandId = None;
                _lastCommandSentBy = None;

                logger.LogInformation("Spotify Remote: Device was changed, stopping playback.");
                try
                {
                    await SendState(PutStateReason.PlayerStateChanged);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to send state");
                }
                finally
                {
                }

                player.Stop();
            }
        };
        player.ShuffleChanged += async (sender, shuffle) =>
        {
            if (player.CurrentStream.IsNone)
                return;

            var currentStream = player.CurrentStream.ValueUnsafe();
            if (currentStream.Stream.Metadata is not ISpotifyPlayableItem metadata)
                return;

            state.PositionAsOfTimestamp = (long)player.Position.ValueUnsafe().TotalMilliseconds;
            state.Timestamp = (long)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            state.Options ??= new ContextPlayerOptions();
            state.Options.ShufflingContext = shuffle;

            await SendState(PutStateReason.PlayerStateChanged);
        };
        player.VolumeChanged += async (sender, vol) =>
        {
            if (player.CurrentStream.IsNone)
                return;

            state.PositionAsOfTimestamp = (long)player.Position.ValueUnsafe().TotalMilliseconds;
            state.Timestamp = (long)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            await SendState(PutStateReason.PlayerStateChanged);
        };
        player.PausedChanged += async (sender, paused) =>
        {
            if (player.CurrentStream.IsNone)
                return;

            var currentStream = player.CurrentStream.ValueUnsafe();
            if (currentStream.Stream.Metadata is not ISpotifyPlayableItem metadata)
                return;

            state.PositionAsOfTimestamp = (long)player.Position.ValueUnsafe().TotalMilliseconds;
            state.Timestamp = (long)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            state.IsPaused = paused;
            state.IsPlaying = true;

            await SendState(PutStateReason.PlayerStateChanged);
        };
        player.PositionChanged += async (sender, e) =>
        {
            if (e.Reason is WaveePlayerPositionChangedEventType.UserRequestedSeeked)
            {
                if (player.CurrentStream.IsNone)
                    return;

                var currentStream = player.CurrentStream.ValueUnsafe();
                if (currentStream.Stream.Metadata is not ISpotifyPlayableItem metadata)
                    return;

                state.PositionAsOfTimestamp = (long)player.Position.ValueUnsafe().TotalMilliseconds;
                state.Timestamp = (long)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();


                await SendState(PutStateReason.PlayerStateChanged);
            }
        };
        player.TrackChanged += async (sender, item) =>
        {
            if (item is not ISpotifyPlayableItem spotifyItem || player.CurrentStream.IsNone)
            {
                playbackId = Option<Guid>.None;
                sessionId = Option<Guid>.None;
                startedPlayingAt = Option<DateTimeOffset>.None;
                return;
            }

            startedPlayingAt.IfNone(() => startedPlayingAt = Option<DateTimeOffset>.Some(DateTimeOffset.UtcNow));
            sessionId.IfNone(() => sessionId = Option<Guid>.Some(Guid.NewGuid()));
            playbackId = Option<Guid>.Some(Guid.NewGuid());

            var currentStream = player.CurrentStream.ValueUnsafe();
            if (player.Context.IsSome)
            {
                var context = player.Context.ValueUnsafe();
                if (context is not ISpotifyContext simpleContext)
                {
                    return;
                }

                state.ContextUri = simpleContext.ContextUri;
                state.ContextUrl = simpleContext.ContextUrl;
                state.ContextRestrictions = new Restrictions();
                state.ContextMetadata.Clear();
                foreach (var (key, value) in simpleContext.ContextMetadata)
                {
                    state.ContextMetadata.Add(key, value);
                }

                state.ContextRestrictions = new Restrictions();
            }

            state.Restrictions = new Restrictions();

            state.PositionAsOfTimestamp = (long)player.Position.ValueUnsafe().TotalMilliseconds;
            state.Timestamp = (long)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            state.PlaybackId = playbackId.ValueUnsafe().ToString();
            state.SessionId = sessionId.ValueUnsafe().ToString();

            state.IsBuffering = false;
            state.IsPlaying = true;
            state.IsPaused = player.Paused;

            state.PlaybackSpeed = 1;
            state.Track = new ProvidedTrack
            {
                Uri = spotifyItem.Uri.ToString(),
                Provider = FindProvider(currentStream.IdInContext),
                Uid = FindUid(currentStream.IdInContext),
            };
            var metadata = FindMetadata(currentStream.IdInContext);
            foreach (var (key, value) in metadata)
            {
                state.Track.Metadata.Add(key, value);
            }

            if (spotifyItem.Uri.Type is AudioItemType.Track)
            {
                state.Track.Metadata["track_player"] = "audio";
            }

            state.Duration = (long)player.Duration.ValueUnsafe().TotalMilliseconds;

            var pageIndex = FindPageIndex(currentStream.IdInContext);
            var itemIndex = FindItemIndex(currentStream.IdInContext);

            var idx = new ContextIndex();
            if (pageIndex.IsSome)
            {
                idx.Page = pageIndex.ValueUnsafe();
            }

            if (itemIndex.IsSome)
            {
                idx.Track = itemIndex.ValueUnsafe();
            }

            state.Index = idx;

            state.Options = new ContextPlayerOptions
            {
                ShufflingContext = player.IsShuffling,
                RepeatingContext = false,
                RepeatingTrack = false
            };

            await SendState(PutStateReason.PlayerStateChanged);
        };

        async Task SendState(PutStateReason reason)
        {
            var volume = player.Volume;
            var putState = new PutStateRequest
            {
                MemberType = MemberType.ConnectState,
                Device = new Device
                {
                    PlayerState = state,
                    DeviceInfo = new DeviceInfo()
                    {
                        CanPlay = true,
                        Volume = (uint)(volume * MAX_VOLUME),
                        Name = _deviceName.Value,
                        DeviceId = _deviceId,
                        DeviceType = _deviceType.Value,
                        DeviceSoftwareVersion = "Spotify-11.1.0",
                        SpircVersion = "3.2.6",
                        Capabilities = new Capabilities
                        {
                            CanBePlayer = true,
                            GaiaEqConnectId = true,
                            SupportsLogout = true,

                            VolumeSteps = (int)VOLUME_STEPS,
                            IsObservable = true,
                            CommandAcks = true,
                            SupportsRename = false,
                            SupportsPlaylistV2 = true,
                            IsControllable = true,
                            SupportsCommandRequest = true,
                            SupportsTransferCommand = true,
                            SupportsGzipPushes = true,
                            NeedsFullPlayerState = true,
                            SupportsHifi = new CapabilitySupportDetails
                            {
                                DeviceSupported = false,
                                FullySupported = false,
                                UserEligible = false
                            }, // TODO: Hifi
                            SupportedTypes =
                            {
                                "audio/episode",
                                "audio/track",
                                //"audio/local"
                            }
                        }
                    }
                },
                PutStateReason = reason,
                ClientSideTimestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                LastCommandMessageId = 0,
                LastCommandSentByDeviceId = string.Empty,
                IsActive = startedPlayingAt.IsSome,
                StartedPlayingAt = (ulong)startedPlayingAt.ValueUnsafe().ToUnixTimeMilliseconds()
            };

            if (_lastCommandId.IsSome)
                putState.LastCommandMessageId = _lastCommandId.ValueUnsafe();
            if (_lastCommandSentBy.IsSome)
            {
                putState.LastCommandSentByDeviceId = _lastCommandSentBy.ValueUnsafe();
            }

            if (!_player.Position.IsSome)
            {
                putState.HasBeenPlayingForMs = 0;
            }
            else
            {
                var diff = (long)putState.ClientSideTimestamp - (long)putState.StartedPlayingAt;
                var min = Math.Min(_player.Position.ValueUnsafe().TotalMilliseconds, diff);
                var max = Math.Max(0, min);
                putState.HasBeenPlayingForMs = (ulong)max;
            }

            if (!EntityManager.TryGetWebsocketConnectionId(_instanceId, out var ws))
                return;
            await _gzipHttpClient.PutState(_httpClient, _tokenFactory,
                ws,
                putState, CancellationToken.None);
        }
    }

    private Option<uint> FindItemIndex(ComposedKey currentStreamIdInContext)
    {
        foreach (var key in currentStreamIdInContext.Keys)
        {
            if (key is SpotifyContextTrackKey x && x.Type is SpotifyContextTrackKeyType.Index)
            {
                return uint.Parse(x.Value);
            }
        }

        return Option<uint>.None;
    }

    private Option<uint> FindPageIndex(ComposedKey currentStreamIdInContext)
    {
        foreach (var key in currentStreamIdInContext.Keys)
        {
            if (key is SpotifyContextTrackKey x && x.Type is SpotifyContextTrackKeyType.PageIndex)
            {
                return uint.Parse(x.Value);
            }
        }

        return None;
    }

    private string FindProvider(ComposedKey currentStreamIdInContext)
    {
        foreach (var key in currentStreamIdInContext.Keys)
        {
            if (key is SpotifyContextTrackKey x && x.Type is SpotifyContextTrackKeyType.Provider)
            {
                return x.Value;
            }
        }

        return string.Empty;
    }

    private string FindUid(ComposedKey currentStreamIdInContext)
    {
        foreach (var key in currentStreamIdInContext.Keys)
        {
            if (key is SpotifyContextTrackKey x && x.Type is SpotifyContextTrackKeyType.Uid)
            {
                return x.Value;
            }
        }

        return string.Empty;
    }

    private HashMap<string, string> FindMetadata(ComposedKey currentStreamIdInContext)
    {
        var metadata = new HashMap<string, string>();
        foreach (var key in currentStreamIdInContext.Keys)
        {
            if (key is SpotifyContextTrackKey x && x.Type is SpotifyContextTrackKeyType.Metadata)
            {
                // ; seperator
                var split = x.Value.Split(';');
                if (split.Length != 2)
                    continue;
                var a = split[0];
                var b = split[1];
                metadata = metadata.Add(a, b);
            }
        }

        return metadata;
    }

    public event EventHandler<RemoteConnectionStatusType> StatusChanged;
    public event EventHandler<Option<SpotifyRemoteState>> StateChanged;
    private event EventHandler<ClusterUpdate> ClusterChanged;
    public Exception LastError { get; private set; }

    public Option<SpotifyRemoteState> State
    {
        get => _state;
        private set
        {
            if (_state == value)
                return;

            _state = value;
            StateChanged?.Invoke(this, value);
        }
    }

    public RemoteConnectionStatusType Status
    {
        get => _status;
        private set
        {
            if (_status == value)
                return;

            _status = value;
            StatusChanged?.Invoke(this, value);
        }
    }

    internal ValueTask Connect(string deviceName, DeviceType deviceType)
    {
        _semaphore.Wait();
        atomic(() => _deviceName.Swap(_ => deviceName));
        atomic(() => _deviceType.Swap(_ => deviceType));

        if (EntityManager.TryGetWebsocketConnection(_instanceId, out var wsConnection))
        {
            // TODO: Possible rename device and change type..
            return ValueTask.CompletedTask;
        }

        var linkedTask = ConnectAsync(_deviceName, _deviceType).ContinueWith(t =>
        {
            _semaphore.Release();
            return t;
        }).Unwrap();

        return new ValueTask(linkedTask);
    }

    private async Task ConnectAsync(Ref<string> deviceName, Ref<DeviceType> deviceType)
    {
        var ws = _websocketClientFactory();
        var dealer = await ApResolve.GetDealer(_httpClient);
        var token = await _tokenFactory();
        var finalUrl = $"wss://{dealer}/?access_token={token}";
        Status = RemoteConnectionStatusType.Connecting;
        await ws.Connect(finalUrl, CancellationToken.None);

        var message = await ws.ReadNextMessage(CancellationToken.None);
        if (message.Type is not SpotifyWebsocketMessageType.ConnectionId)
        {
            throw new Exception("Expected connection id");
        }

        var connectionId = Encoding.UTF8.GetString(message.Payload.Span);
        var putState = new PutStateRequest
        {
            MemberType = MemberType.ConnectState,
            Device = new Device
            {
                DeviceInfo = new DeviceInfo()
                {
                    CanPlay = true,
                    Volume = (uint)(0.5 * MAX_VOLUME),
                    Name = deviceName.Value,
                    DeviceId = _deviceId,
                    DeviceType = deviceType.Value,
                    DeviceSoftwareVersion = "Spotify-11.1.0",
                    SpircVersion = "3.2.6",
                    Capabilities = new Capabilities
                    {
                        CanBePlayer = true,
                        GaiaEqConnectId = true,
                        SupportsLogout = true,

                        VolumeSteps = (int)VOLUME_STEPS,
                        IsObservable = true,
                        CommandAcks = true,
                        SupportsRename = false,
                        SupportsPlaylistV2 = true,
                        IsControllable = true,
                        SupportsCommandRequest = true,
                        SupportsTransferCommand = true,
                        SupportsGzipPushes = true,
                        NeedsFullPlayerState = true,
                        SupportsHifi = new CapabilitySupportDetails
                        {
                            DeviceSupported = false,
                            FullySupported = false,
                            UserEligible = false
                        }, // TODO: Hifi
                        SupportedTypes =
                        {
                            "audio/episode",
                            "audio/track",
                            //"audio/local"
                        }
                    }
                }
            },
            PutStateReason = PutStateReason.NewDevice,
            ClientSideTimestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            LastCommandMessageId = 0,
            LastCommandSentByDeviceId = string.Empty
        };

        var cluster = await _gzipHttpClient.PutState(_httpClient, _tokenFactory, connectionId, putState,
            CancellationToken.None);
        (await cluster.ToRemoteState(_instanceId)).IfSucc(x => State = x);
        EntityManager.SaveWebsocketConnection(_instanceId, ws, connectionId);

        new Thread(async () =>
        {
            try
            {
                Status = RemoteConnectionStatusType.Connected;
                _logger.LogInformation(
                    "Spotify WebSocket connected, should appear in Spotify now as: \"{DeviceName}\" ({DeviceType})",
                    deviceName.Value, deviceType.Value);
                while (true)
                {
                    if (!EntityManager.TryGetWebsocketConnection(_instanceId, out var ws))
                    {
                        throw new Exception("Connection was closed unexpectedly.");
                    }

                    var msg = await ws.ReadNextMessage(cancellationToken: CancellationToken.None);
                    if (msg.Type is SpotifyWebsocketMessageType.Pong)
                    {
                        _pongTcs.TrySetResult();
                    }
                    else if (msg.Type is SpotifyWebsocketMessageType.Message)
                    {
                        var isClusterUpdate = msg.Url.Equals("hm://connect-state/v1/cluster");
                        if (isClusterUpdate)
                        {
                            var update = ClusterUpdate.Parser.ParseFrom(msg.Payload.Span);
                            ClusterChanged?.Invoke(this, update);
                            (await update.Cluster.ToRemoteState(_instanceId)).IfSucc(x => State = x);
                        }
                        else if (msg.Url.Equals("hm://connect-state/v1/connect/volume"))
                        {
                            var setVolumeCmd = SetVolumeCommand.Parser.ParseFrom(msg.Payload.Span);
                            var volume = setVolumeCmd.Volume;
                            var maxVolume = WaveeSpotifyRemoteClient.MAX_VOLUME;
                            var volumePercent = volume / (double)maxVolume;
                            _player.SetVolume((float)volumePercent);
                            _lastCommandId = Some((uint)setVolumeCmd.CommandOptions.MessageId);
                        }
                    }
                    else if (msg.Type is SpotifyWebsocketMessageType.Request)
                    {
                        using var jsonDoc = JsonDocument.Parse(msg.Payload);
                        var messageId = jsonDoc.RootElement.GetProperty("message_id").GetUInt32();
                        var sentByDeviceId = jsonDoc.RootElement.GetProperty("sent_by_device_id").GetString();
                        var cmd = jsonDoc.RootElement.GetProperty("command");
                        var endpoint = cmd.GetProperty("endpoint").GetString();
                        _lastCommandId = messageId;
                        _lastCommandSentBy = sentByDeviceId;
                        _logger.LogDebug("Received command {Endpoint} from {DeviceId}", endpoint, sentByDeviceId);
                        switch (endpoint)
                        {
                            case "set_shuffling_context":
                                {
                                    var val = cmd.GetProperty("value").GetBoolean();
                                    _player.SetShuffling(val);
                                    break;
                                }
                            case "set_queue":
                                {
                                    break;
                                }
                            case "update_context":
                                {
                                    var ctx = Eum.Spotify.context.Context.Parser.ParseJson(cmd.GetProperty("context")
                                        .GetRawText());
                                    if (_player.Context.IsSome)
                                    {
                                        var currentContext = _player.Context.ValueUnsafe();
                                        if (currentContext is ISpotifyContext spotifyCtx &&
                                            spotifyCtx.ContextUri == ctx.Uri)
                                        {
                                            await spotifyCtx.RefreshContext(ctx, true);
                                        }
                                    }

                                    break;
                                }
                            case "skip_prev":
                                {
                                    _player.SkipPrevious();
                                    break;
                                }
                            case "skip_next":
                                {
                                    _player.SkipNext();
                                    break;
                                }
                            case "play":
                                {
                                    await PlayHandler.HandlePlay(cmd, _instanceId);
                                    break;
                                }
                            case "pause":
                                {
                                    _player.Pause();
                                    break;
                                }
                            case "resume":
                                {
                                    _player.Resume();
                                    break;
                                }
                            case "seek_to":
                                {
                                    var value = cmd.GetProperty("value").GetDouble();
                                    _player.SeekToPosition(TimeSpan.FromMilliseconds(value));
                                    break;
                                }
                            case "transfer":
                                {
                                    var transfer =
                                        TransferState.Parser.ParseFrom(cmd.GetProperty("data").GetBytesFromBase64());
                                    await RemoteTransfer.HandleTransfer(transfer, _instanceId);
                                    break;
                                }
                            default:
                                {
                                    Debugger.Break();
                                    break;
                                }
                        }

                        var reply = string.Format(
                            "{{\"type\":\"reply\", \"key\": \"{0}\", \"payload\": {{\"success\": {1}}}}}",
                            msg.Key, "true");
                        await ws.SendJson(reply, CancellationToken.None);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Spotify WebSocket failed unexpectedly.");

                if (e is WebSocketException)
                {
                    Status = RemoteConnectionStatusType.NotConnectedDueToError;
                    LastError = e;
                }
                else
                {
                    Status = RemoteConnectionStatusType.NotConnected;
                }
            }

            State = Option<SpotifyRemoteState>.None;

            if (!_manualClose)
            {
                _logger.LogInformation("Spotify WebSocket closed. Reconnecting...");
                EntityManager.RemoveWebsocketConnection(_instanceId);
                await ConnectAsync(deviceName, deviceType);
            }
        }).Start();
    }

    public Task<bool> Resume(bool waitForResponse)
    {
        var command = new { command = new { endpoint = "resume" } };
        return SendCommand(command, waitForResponse);
    }


    public Task<bool> Pause(bool waitForResponse)
    {
        var command = new { command = new { endpoint = "pause" } };
        return SendCommand(command, waitForResponse);
    }
    public Task<bool> SkipNext(bool waitForResponse)
    {
        var command = new { command = new { endpoint = "skip_next" } };
        return SendCommand(command, waitForResponse);
    }
    public Task<bool> SkipPrev(bool waitForResponse)
    {
        var command = new { command = new { endpoint = "skip_prev" } };
        return SendCommand(command, waitForResponse);
    }

    public Task<bool> SeekTo(TimeSpan position, bool waitForResponse)
    {
        var command = new { command = new { endpoint = "seek_to", value = position.TotalMilliseconds } };
        return SendCommand(command, waitForResponse);
    }

    public Task<bool> SetShuffle(bool isShuffling, bool waitForResponse)
    {
        var command = new { command = new { endpoint = "set_shuffling_context", value = isShuffling } };
        return SendCommand(command, waitForResponse);
    }
    public Task<bool> GoToRepeatState(WaveeRepeatStateType repeatState, bool waitForResponse)
    {
        var command = new
        {
            command = new
            {
                endpoint = "set_options",
                repeating_context = repeatState >= WaveeRepeatStateType.Context,
                repeating_track = repeatState >= WaveeRepeatStateType.Track
            }
        };
        return SendCommand(command, waitForResponse);
    }

    public async Task<bool> SetVolume(double oneToZero, bool waitForResponse)
    {
        var activeDeviceId = _state.Bind(x => x.ActiveDeviceId);
        if (activeDeviceId.IsNone) return false;

        var token = await _tokenFactory();
        try
        {
            using var response = await _httpClient.SendVolumeCommand(token, new
            {
                volume = Math.Min(oneToZero * MAX_VOLUME, MAX_VOLUME)
            },
                fromDeviceId: _deviceId,
                activeDeviceId.ValueUnsafe());
            response.EnsureSuccessStatusCode();
            if (!waitForResponse) return true;

            await using var ack = await response.Content.ReadAsStreamAsync();
            using var json = await JsonDocument.ParseAsync(ack);
            var ackId = json.RootElement.GetProperty("ack_id").GetString();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while sending pause command");
            return false;
        }
    }

    private async Task<bool> SendCommand(object command, bool waitForResponse)
    {
        var activeDeviceId = _state.Bind(x => x.ActiveDeviceId);
        if (activeDeviceId.IsNone) return false;

        var token = await _tokenFactory();
        try
        {
            using var response = await _httpClient.SendCommand(token, command,
                fromDeviceId: _deviceId,
                activeDeviceId.ValueUnsafe());
            response.EnsureSuccessStatusCode();
            if (!waitForResponse) return true;

            await using var ack = await response.Content.ReadAsStreamAsync();
            using var json = await JsonDocument.ParseAsync(ack);
            var ackId = json.RootElement.GetProperty("ack_id").GetString();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while sending pause command");
            return false;
        }
    }

}

public readonly record struct SpotifyRemoteState
{
    public required Option<Context> Item { get; init; }
    public required Option<SpotifySimpleContext> Context { get; init; }
    public required Seq<DeviceInfo> Devices { get; init; }
    public required Option<string> ActiveDeviceId { get; init; }
    public TimeSpan Position => PositionStopwatch.Elapsed + PositionOffset;
    public required bool IsPaused { get; init; }
    public required WaveeRepeatStateType RepeatState { get; init; }
    public required bool IsShuffling { get; init; }

    public required
        HashMap<SpotifyRestrictionAppliesForType,
            Seq<SpotifyKnownRestrictionType>> Restrictions
    { get; init; }

    public Stopwatch PositionStopwatch { get; init; }
    public TimeSpan PositionOffset { get; init; }


    public override string ToString()
    {
        //Shorten the string to make it easier to read in the console
        var sb = new StringBuilder();
        sb.Append("SpotifyRemoteState { ");
        sb.Append($"Item: {ToString(Item)}, ");
        sb.Append($"Context: {ToString(Context)}, ");
        sb.Append($"Devices: {ToString(Devices)}, ");
        sb.Append($"ActiveDevice: {ToString(Devices, ActiveDeviceId)}, ");
        sb.Append($"Position: {Position}, ");
        sb.Append($"IsPaused: {IsPaused}, ");
        sb.Append($"RepeatState: {RepeatState}, ");
        sb.Append($"IsShuffling: {IsShuffling}, ");
        sb.Append($"Restrictions: {ToString(Restrictions)}, ");
        sb.Append('}');

        return sb.ToString();
    }

    private string ToString(Option<SpotifySimpleContext> ctx)
    {
        if (ctx.IsNone)
            return "None";

        var val = ctx.ValueUnsafe();
        var sb = new StringBuilder();
        // { Uri: {Uri}, Name: {Item.Name or Unknown} }
        sb.Append('{');
        sb.Append("Uri: ");
        sb.Append(val.Uri);
        sb.Append(", ");
        sb.Append("Name: ");
        sb.Append(val.Item.Match(x => x.Name, () => "Unknown"));
        sb.Append('}');
        return sb.ToString();
    }

    private string ToString(Option<Context> ctx)
    {
        if (ctx.IsNone)
            return "None";

        var val = ctx.ValueUnsafe();
        var sb = new StringBuilder();
        // { Uid: {Uri}, ItemIndex: .. or Unknown, PageIndex: .. or Unknown }

        sb.Append('{');
        sb.Append("Uri: ");
        sb.Append(val.Item.Uri);
        sb.Append(", ");
        sb.Append("Name: ");
        sb.Append(val.Item.Name);
        sb.Append(", ");
        sb.Append("Uid: ");
        sb.Append(val.Uid.Match(x => x, () => "Unknown"));
        sb.Append(", ");
        sb.Append("ItemIndex: ");
        sb.Append(val.ItemIndex.Match(x => x.ToString(), () => "Unknown"));
        sb.Append(", ");
        sb.Append("PageIndex: ");
        sb.Append(val.PageIndex.Match(x => x.ToString(), () => "Unknown"));
        sb.Append('}');
        return sb.ToString();
    }

    private string ToString(Seq<DeviceInfo> restrictions)
    {
        // only names
        var sb = new StringBuilder();
        foreach (var restriction in restrictions)
        {
            sb.Append('"');
            sb.Append(restriction.Name);
            sb.Append('"');
            sb.Append(", ");
        }

        return sb.ToString();
    }

    private string ToString(Seq<DeviceInfo> activeDevices, Option<string> activeDeviceId)
    {
        // find name of active device
        if (activeDeviceId.IsNone)
            return "None";

        foreach (var device in activeDevices)
        {
            if (device.DeviceId == activeDeviceId)
            {
                return device.Name;
            }
        }

        return "Unknown";
    }

    private string ToString(HashMap<SpotifyRestrictionAppliesForType, Seq<SpotifyKnownRestrictionType>> restrictions)
    {
        // ({Type} (Count))
        var sb = new StringBuilder();
        foreach (var restriction in restrictions)
        {
            sb.Append('[');
            sb.Append(restriction.Key);
            sb.Append(' ');

            sb.Append('(');
            var restrictionTypes = restriction.Value.Count;
            sb.Append(restrictionTypes);
            sb.Append(')');

            sb.Append(']');
            sb.Append(' ');
        }

        return sb.ToString();
    }
}

public enum RemoteConnectionStatusType
{
    NotConnected,
    Connecting,
    Connected,
    NotConnectedDueToError
}

public enum SpotifyKnownRestrictionType
{
    NotPaused,
    EndlessContext,
    Dj,
    Narration,
    NoPreviousTrack,
    Radio,
    AutoPlay,
    NotPlayingMedia,
    Unknown
}

public enum SpotifyRestrictionAppliesForType
{
    Shuffle,
    SkippingNext,
    SkippingPrevious,
    RepeatContext,
    RepeatTrack,
    Pausing,
    Resuming,
    Seeking,
}