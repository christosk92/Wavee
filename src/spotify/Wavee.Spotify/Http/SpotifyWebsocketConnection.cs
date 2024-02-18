using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using Wavee.Spotify.Extensions;
using Wavee.Spotify.Http.Interfaces;

namespace Wavee.Spotify.Http;

internal sealed class SpotifyWebsocketConnection : IDisposable
{
    private readonly ClientWebSocket _clientWebSocket;
    private readonly IAPIConnector _api;

    internal Cluster? _cluster;
    private string? _connectionId;


    private readonly Subject<(Exception?, WebSocketCloseStatus?)> _onDisconnected;
    private readonly Subject<(Cluster, string)> _clusterChanged;

    public SpotifyWebsocketConnection(ClientWebSocket clientWebSocket, IAPIConnector api,
        Subject<(Cluster, string)> clusterChanged, Subject<(Exception?, WebSocketCloseStatus?)> onDisconnected)
    {
        _connectionId = null;
        _clientWebSocket = clientWebSocket;
        _api = api;
        _clusterChanged = clusterChanged;
        _onDisconnected = onDisconnected;
    }

    public ValueTask<string> ConnectionId(CancellationToken cancellationToken)
    {
        if (_connectionId != null)
        {
            return new ValueTask<string>(_connectionId);
        }

        return new ValueTask<string>(FetchConnectionIdAsync(cancellationToken));
    }

    public IObservable<Cluster> ClusterChanged => _clusterChanged
        .Where(x => x.Item2 == _connectionId)
        .Select(x => x.Item1);

    public IObservable<(Exception?, WebSocketCloseStatus?)> OnDisconnected => _onDisconnected;

    private async Task<string> FetchConnectionIdAsync(CancellationToken cancellationToken)
    {
        await using var message = await Receive(cancellationToken);
        using var jsondoc = await JsonDocument.ParseAsync(message, cancellationToken: cancellationToken);
        var headers = jsondoc.RootElement.GetProperty("headers");
        var connectionIdVal = headers.GetProperty("Spotify-Connection-Id").GetString();
        if (connectionIdVal == null)
        {
            throw new InvalidOperationException("Spotify-Connection-Id header not found");
        }

        _connectionId = connectionIdVal;

        await Task.Factory.StartNew(async () =>
        {
            try
            {
                while (true)
                {
                    using var msg = await ReadNextMessageAsync(cancellationToken);
                    var uri = msg.RootElement.GetProperty("uri").GetString();
                    var messageHeaders = new Dictionary<string, string>();
                    if (msg.RootElement.TryGetProperty("headers", out var headersElement))
                    {
                        using var enumerator = headersElement.EnumerateObject();
                        // headers = enumerator.Fold(headers, (acc, curr) => acc.Add(curr.Name, curr.Value.GetString()));
                        foreach (var curr in enumerator)
                        {
                            messageHeaders.Add(curr.Name, curr.Value.GetString()!);
                        }
                    }

                    if (uri is "hm://connect-state/v1/cluster")
                    {
                        var payload = ReadPayload(msg.RootElement, messageHeaders);
                        var clusterUpdate = ClusterUpdate.Parser.ParseFrom(payload.Span);
                        _cluster = clusterUpdate.Cluster;
                        _clusterChanged.OnNext((_cluster, _connectionId));
                    }

                    Console.WriteLine("Incoming message: " + uri);
                }
            }
            catch (Exception e)
            {
                _onDisconnected.OnNext((e, null));
            }
        }, TaskCreationOptions.LongRunning);

        return connectionIdVal;
    }

    private async Task<JsonDocument> ReadNextMessageAsync(CancellationToken cancellationToken)
    {
        await using var message = await Receive(cancellationToken);
        var jsondoc = await JsonDocument.ParseAsync(message, cancellationToken: cancellationToken);

        return jsondoc;
    }

    public async Task UpdateState(string deviceId,
        string deviceName,
        DeviceType deviceType,
        PutStateReason reason,
        PlayerState? state,
        DateTimeOffset? startedPlayingAt,
        CancellationToken cancel)
    {
        const uint VOLUME_STEPS = 12;
        const uint MAX_VOLUME = 65535;
        var putState = new PutStateRequest
        {
            MemberType = MemberType.ConnectState,
            Device = new Device
            {
                DeviceInfo = new DeviceInfo()
                {
                    CanPlay = true,
                    Volume = (uint)(0.5 * MAX_VOLUME),
                    Name = deviceName,
                    DeviceId = deviceId,
                    DeviceType = deviceType,
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
            LastCommandSentByDeviceId = string.Empty
        };
        if (startedPlayingAt != null)
        {
            putState.StartedPlayingAt = (ulong)startedPlayingAt.Value.ToUnixTimeMilliseconds();
            putState.IsActive = true;
        }

        if (state != null)
        {
            putState.Device.PlayerState = state;
        }

        var connectionId = await ConnectionId(cancel);
        var putstateUrl = $"https://gae2-spclient.spotify.com/connect-state/v1/devices/{deviceId}";
        var headers = new Dictionary<string, string>
        {
            { "X-Spotify-Connection-Id", connectionId! }
        };
        var content = new ByteArrayContent(putState.ToByteArray());
        var cluster =
            await _api.Put<Cluster>(new Uri(putstateUrl), headers, content, RequestContentType.Protobuf, cancel);
        _cluster = cluster;
        _clusterChanged.OnNext((cluster, connectionId));
    }

    public async Task<Stream> Receive(CancellationToken cancellationToken = default)
    {
        var message = new MemoryStream();
        bool endOfMessage = false;
        while (!endOfMessage)
        {
            var buffer = new byte[1024 * 4];
            var segment = new ArraySegment<byte>(buffer);
            var result = await _clientWebSocket.ReceiveAsync(segment, cancellationToken: cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                throw new WebSocketException(error: WebSocketError.ConnectionClosedPrematurely);
            }

            message.Write(buffer, 0, result.Count);
            endOfMessage = result.EndOfMessage;
        }

        message.Seek(0, SeekOrigin.Begin);

        return message;
    }

    public void Dispose()
    {
        _clientWebSocket.Dispose();
    }

    internal readonly record struct SpotifyWebsocketMessage(string Uri);

    public Task Close(WebSocketCloseStatus normalClosure, string manuallyClosed)
    {
        return _clientWebSocket.CloseAsync(normalClosure, manuallyClosed, CancellationToken.None);
    }

    private static ReadOnlyMemory<byte> ReadPayload(JsonElement messageRootElement, Dictionary<string, string> headers)
    {
        Memory<byte> payload = Memory<byte>.Empty;
        var gzip = false;
        var plainText = false;
        if (headers.TryGetValue("Transfer-Encoding", out var trnsfEncoding))
        {
            if (trnsfEncoding is "gzip")
            {
                gzip = true;
            }
        }

        if (headers.TryGetValue("Content-Type", out var cntEncoding))
        {
            if (cntEncoding is "text/plain")
            {
                plainText = true;
            }
        }

        if (messageRootElement.TryGetProperty("payloads", out var payloadsArr))
        {
            var payloads = new ReadOnlyMemory<byte>[payloadsArr.GetArrayLength()];
            for (var i = 0; i < payloads.Length; i++)
            {
                if (plainText)
                {
                    ReadOnlyMemory<byte> bytes = Encoding.UTF8.GetBytes(payloadsArr[i].GetString());
                    payloads[i] = bytes;
                }
                else
                {
                    payloads[i] = payloadsArr[i].GetBytesFromBase64();
                }
            }

            var totalLength = payloads.Sum(p => p.Length);
            payload = new byte[totalLength];
            var offset = 0;
            foreach (var payloadPart in payloads)
            {
                payloadPart.CopyTo(payload.Slice(offset));
                offset += payloadPart.Length;
            }
        }
        else if (messageRootElement.TryGetProperty("payload", out var payloadStr))
        {
            if (gzip is true)
            {
                payload = payloadStr.GetProperty("compressed").GetBytesFromBase64();
            }
            else
            {
                payload = payloadStr.GetBytesFromBase64();
            }
        }
        else
        {
            payload = Memory<byte>.Empty;
        }

        switch (gzip)
        {
            case false:
                //do nothing
                break;
            case true:
            {
                payload = Gzip.UnsafeDecompressAltAsMemory(payload.Span);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }


        return payload;
    }
}