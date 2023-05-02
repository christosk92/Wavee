using System.Buffers;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Channels;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using LanguageExt.Common;
using LanguageExt.UnsafeValueAccess;
using Wavee.Spotify.Infrastructure.Sys.IO;
using Wavee.Spotify.Infrastructure.Traits;
using Wavee.Spotify.Remote.Helpers.Extensions;
using Wavee.Spotify.Remote.Infrastructure.Sys;
using Wavee.Spotify.Remote.Infrastructure.Sys.IO;
using Wavee.Spotify.Remote.Models;
using Wavee.Spotify.Remote.State;
using Wavee.States;
using TransferState = Eum.Spotify.transfer.TransferState;

namespace Wavee.Spotify.Remote.Infrastructure.Live;

public sealed class LiveSpotifyRemote : ISpotifyRemote
{
    private CancellationTokenSource _ct;
    private readonly SpotifyPlaybackConfig _config;
    private readonly SpotifyRemoteSession<Runtime> _session;
    private readonly ISpotifyClient _client;

    private readonly ManualResetEvent _connectionEstablished = new(false);
    private readonly Ref<Option<Cluster>> _cluster = Ref(Option<Cluster>.None);
    private readonly Ref<Option<string>> _connectionId = Ref(Option<string>.None);
    private readonly Ref<SpotifyRemoteState<Runtime>> _remoteState;
    private readonly ChannelWriter<JsonDocument> _sendQueue; //TODO: Replace this with a functional queue

    private readonly Runtime _rt;

    private readonly IWaveePlayer _waveePlayer;

    public LiveSpotifyRemote(
        ISpotifyClient client,
        IWaveePlayer waveePlayer,
        SpotifyPlaybackConfig config)
    {
        var sendQueue = Channel.CreateUnbounded<JsonDocument>();
        _sendQueue = sendQueue.Writer;

        _rt = Runtime.New();
        _remoteState = Ref(new SpotifyRemoteState<Runtime>(config, client.Config.DeviceId, _rt));
        _ct = new CancellationTokenSource();
        _config = config;
        _waveePlayer = waveePlayer;
        _client = client;
        _session = new SpotifyRemoteSession<Runtime>(_rt);

        _connectionId.OnChange()
            .Subscribe(ConnectionIdChanged);

        Task.Factory.StartNew(async () =>
        {
            await foreach (var packet in sendQueue.Reader.ReadAllAsync(_ct.Token))
            {
                //we have a connection, so we need to read from it
                var aff =
                    from _ in Ws<Runtime>
                        .SendText(packet.RootElement.GetRawText())
                    select unit;

                var result = await aff.Run(_rt);
            }
        });


        _waveePlayer.StateChanged.Subscribe(async state =>
        {
            SpotifyRemoteState<Runtime> changeState = _remoteState.Value;
            switch (state)
            {
                case WaveePlayingState _:
                    changeState = atomic(() =>
                    {
                        return _remoteState.Swap(x =>
                        {
                            var wasPaused = x.IsPaused;
                            var newPos = wasPaused
                                ? x.PositionAsOfTimestamp.Map(x => (long)x)
                                : x.Position;
                            return x with
                            {
                                IsPlaying = true,
                                IsPaused = false,
                                Position = newPos
                            };
                        });
                    });
                    break;
            }

            var token = await _client.Token.GetToken();
            var run = await PutState(
                    changeState.BuildPutStateRequest(PutStateReason.PlayerStateChanged,
                        _waveePlayer.Position.Map(x => (ulong)x.TotalMilliseconds)),
                    token,
                    _connectionId.Value.ValueUnsafe())
                .Run(_rt);
            run.Match(
                Succ: async message =>
                {
                    var bytesResponse = await message.Content.ReadAsByteArrayAsync();
                    var cluster = Cluster.Parser.ParseFrom(bytesResponse);
                    atomic(() => _cluster.Swap(x => cluster));
                },
                Fail: ex => Debug.WriteLine(ex.Message)
            );
        });
    }

    public Option<string> ConnectionId => _connectionId;

    public async ValueTask Connect()
    {
        try
        {
            _ct.Cancel();
        }
        catch (Exception)
        {
            // ignored
        }

        try
        {
            _ct.Dispose();
        }
        catch (Exception)
        {
            // ignored
        }

        _ct = new CancellationTokenSource();

        const string dealer = "wss://gae2-dealer.spotify.com:443/?access_token={0}";

        Func<Error, Aff<Runtime, Unit>> onError = error => Eff<Runtime, Unit>((_) =>
        {
            Debug.WriteLine(error);
            return unit;
        });

        var aff =
            from token in _client.Token.GetToken().ToAff()
            from _ in Ws<Runtime>.Connect(string.Format(dealer, token))
            select unit;

        var run = await aff.Run(_session.Runtime);

        // Start the Listen method asynchronously
        _ = Task.Run(() => Ws<Runtime>
            .Listen(
                msgData => HandleMessage(
                    message: msgData,
                    cluster: _cluster,
                    remoteState: _remoteState,
                    onMsgSend: document => _sendQueue.TryWrite(document),
                    swapConId: _connectionId,
                    player: _waveePlayer),
                onError, _ct.Token)
            .Run(_session.Runtime));
    }

    public IObservable<Option<string>> OnConnectionIdChanged => _connectionId.OnChange();

    private async void ConnectionIdChanged(Option<string> newConnId)
    {
        //notify spotify client of new device (us!)
        var async = await newConnId.MatchAsync(
            None: () =>
            {
                //reset states
                _connectionEstablished.Reset();
                return unit;
            },
            Some: async s1 =>
            {
                var putstate = _remoteState.Value.BuildPutStateRequest(PutStateReason.NewDevice, None);
                var token = await _client.Token.GetToken();
                var run = await PutState(putstate, token, s1).Run(runtime: _rt);
                if (run.IsSucc)
                {
                    var message = run.Match(Succ: responseMessage => responseMessage, Fail: ex => throw ex);
                    var content = await message.Content.ReadAsByteArrayAsync();
                    var cluster = Cluster.Parser.ParseFrom(content);
                    atomic(() => _cluster.Swap(x => cluster));
                }
                else
                {
                    var error = run.Match(Succ: responseMessage => throw new Exception("Unexpected success"),
                        Fail: ex => ex);
                    Debug.WriteLine(error.Message);
                }

                _connectionEstablished.Set();
                return unit;
            }
        );
    }

    private static Aff<Runtime, Unit> HandleMessage(
        ReadOnlyMemory<byte> message,
        Ref<Option<Cluster>> cluster,
        Ref<SpotifyRemoteState<Runtime>> remoteState,
        Action<JsonDocument> onMsgSend,
        Ref<Option<string>> swapConId,
        IWaveePlayer player)
    {
        using var jsonDocument = JsonDocument.Parse(message);
        var rootOriginal = jsonDocument.RootElement;
        var root = rootOriginal.Clone();

        var type = root.TryGetProperty("type", out var t) ? Some(t.GetString()!) : None;
        var headers = root.TryGetProperty("headers", out var h) ? Some(h) : None;
        var uri = root.TryGetProperty("uri", out var u) ? Some(u.GetString()!) : None;
        var payloads = root.TryGetProperty("payloads", out var p) ? Some(p) : None;


        return type.Match(
            None: () => FailEff<Unit>(Error.New("Expected type to be present but instead got None")),
            Some: typeValue =>
            {
                switch (typeValue)
                {
                    case "request":
                        return HandleRequest(root, remoteState, onMsgSend, player);
                    default:
                        var uriResult = uri.ToEff(Error.New("Expected uri to be present but instead got None"));

                        return uriResult.Bind(uriReceived =>
                        {
                            if (uriReceived.StartsWith("hm://pusher/v1/connections/"))
                            {
                                return HandlePusherConnection(headers, swapConId);
                            }

                            if (uriReceived.StartsWith("hm://connect-state/v1/cluster"))
                            {
                                return HandleClusterUpdate(headers, payloads, cluster);
                            }
                            else
                            {
                                return FailEff<Runtime, Unit>(Error.New($"Unexpected uri received: {uriReceived}"));
                            }
                        });
                }
            }
        );
    }

    private static Aff<Runtime, Unit> HandleRequest(
        JsonElement root,
        Ref<SpotifyRemoteState<Runtime>> remoteState,
        Action<JsonDocument> onMessageSend,
        IWaveePlayer player)
    {
        return
            from headers in root.GetRequiredProperty("headers")
            from payload in root.GetRequiredProperty("payload")
            from key in root.GetRequiredProperty("key").Map(k => k.GetString()!)
            from transferEncodingEff in headers.GetRequiredProperty("Transfer-Encoding").Map(e => e.ToString())
                .Map(x => x switch
                {
                    "gzip" => DecodeGzipPayload(payload),
                    _ => FailEff<ReadOnlyMemory<byte>>(Error.New($"Unexpected transfer-encoding: {x}"))
                })
            from decodedData in transferEncodingEff.Map(ParsePayload)
            from res in HandleData(decodedData, remoteState, player)
            from _ in Eff(() =>
            {
                const bool success = true;
                var keyStr = key;
                var reply =
                    $"{{\"type\":\"reply\", \"key\": \"{keyStr.ToLower()}\", \"payload\": {{\"success\": {success.ToString().ToLowerInvariant()}}}}}";
                var replyJson = JsonDocument.Parse(reply);
                onMessageSend(replyJson);
                return unit;
            })
            select unit;
    }

    private static Aff<Runtime, Unit> HandleData(
        SpotifyRequestMessage decodedData,
        Ref<SpotifyRemoteState<Runtime>> remoteState,
        IWaveePlayer player)
    {
        atomic(() => remoteState.Swap(x => x with
        {
            LastCommandId = decodedData.MessageId,
            LastCommandSentByDeviceId = decodedData.SentByDeviceId
        }));
        var endpoint = decodedData.Endpoint;
        if (endpoint.IsNone)
        {
            return FailEff<Runtime, Unit>(Error.New("Expected endpoint to be present but instead got None"));
        }

        switch (endpoint.ValueUnsafe())
        {
            case "transfer":
                //transfer state is enclosed in command.data
                //we also have options inside command.options
                ReadOnlySpan<byte> transferStateElem =
                    decodedData.Command.ValueUnsafe().GetProperty("data").GetBytesFromBase64();
                var options = decodedData.Command.ValueUnsafe().GetProperty("options");
                var transferState = TransferState.Parser.ParseFrom(transferStateElem);
                return
                    from swappedState in atomic(transferState.OnTransfer(remoteState, options))
                    from ctx in swappedState.BuildContext()
                    from _ in player.Play(ctx.ValueUnsafe(),
                        Option<int>.None,
                        TimeSpan.FromMilliseconds(swappedState.GetPosition()),
                        !swappedState.IsPaused).ToAff()
                    select unit;
        }

        return SuccessEff(unit);
    }

    private static SpotifyRequestMessage ParsePayload(ReadOnlyMemory<byte> decodedData)
    {
        using var jsonDocument = JsonDocument.Parse(decodedData);
        var root = jsonDocument.RootElement;
        var messageId = root.TryGetProperty("message_id", out var m) ? Some(m.GetUInt32()!) : None;
        var sentByDeviceId = root.TryGetProperty("sent_by_device_id", out var s)
            ? Some(s.GetString()!)
            : None;
        var command = root.TryGetProperty("command", out var c) ? Some(c.Clone()) : None;
        var endpoint = command.Match(
            None: () => None,
            Some: cValue => cValue.TryGetProperty("endpoint", out var e) ? Some(e.GetString()!) : None
        );


        return new SpotifyRequestMessage(messageId, sentByDeviceId, command, endpoint);
    }

    private static Eff<Runtime, Unit>
        HandlePusherConnection(Option<JsonElement> headers, Ref<Option<string>> swapConId) =>
        from h in headers.ToEff(Error.New("Expected headers to be present but instead got None"))
        let connId = h.TryGetProperty("Spotify-Connection-Id", out var c)
            ? Some(c.GetString()!)
            : None
        from __ in Eff(() =>
        {
            atomic(() => swapConId.Swap(k => connId));
            return unit;
        })
        select __;

    private static Eff<Runtime, Unit> HandleClusterUpdate(Option<JsonElement> headers, Option<JsonElement> payloads,
        Ref<Option<Cluster>> cluster) =>
        from p in payloads.ToEff(Error.New("Expected payloads to be present but instead got None"))
        from header in headers.ToEff(Error.New("Expected headers to be present but instead got None"))
        let transferEncoding = header.TryGetProperty("Transfer-Encoding", out var trf)
            ? Some(trf.ToString())
            : None
        from decodedData in transferEncoding == "gzip"
            ? DecodeGzipPayload(p)
            : FailEff<Runtime, ReadOnlyMemory<byte>>(Error.New($"Unexpected transfer-encoding: {transferEncoding}"))
        let clusterUpdate = ClusterUpdate.Parser.ParseFrom(decodedData.Span).Cluster
        from __ in Eff(() =>
        {
            Debug.WriteLine("Cluster received");
            return atomic(() => cluster.Swap(x => Some(clusterUpdate)));
        })
        select unit;


    private static Eff<Runtime, ReadOnlyMemory<byte>> DecodeGzipPayload(JsonElement p)
    {
        return Eff<Runtime, ReadOnlyMemory<byte>>((_) =>
        {
            ReadOnlySpan<byte> base64 = ReadOnlySpan<byte>.Empty;

            if (p.ValueKind == JsonValueKind.Array)
            {
                using var enumerateArray = p.EnumerateArray();
                base64 = enumerateArray.First().GetBytesFromBase64();
            }
            else if (p.ValueKind == JsonValueKind.Object)
            {
                base64 = p.GetProperty("compressed").GetBytesFromBase64();
            }
            else
            {
                throw new ArgumentException("Invalid JSON element type");
            }

            // Use a buffer pool to reduce memory allocations
            var bufferPool = ArrayPool<byte>.Shared;
            var buffer = bufferPool.Rent(4096);

            try
            {
                var bytesRead = 0;
                using var ms = new MemoryStream(base64.ToArray());
                using var gzip = new GZipStream(ms, CompressionMode.Decompress);

                while (true)
                {
                    var read = gzip.Read(buffer.AsSpan(bytesRead));
                    if (read == 0)
                    {
                        break;
                    }

                    bytesRead += read;

                    // Resize the buffer if necessary
                    if (bytesRead >= buffer.Length)
                    {
                        var newBuffer = bufferPool.Rent(buffer.Length * 2);
                        buffer.AsSpan().CopyTo(newBuffer);
                        bufferPool.Return(buffer);
                        buffer = newBuffer;
                    }
                }

                // Create a ReadOnlyMemory<byte> with the exact size of the decompressed data
                var result = new byte[bytesRead];
                buffer.AsSpan(0, bytesRead).CopyTo(result);
                return result.AsMemory();
            }
            finally
            {
                bufferPool.Return(buffer);
            }
        });
    }

    private static Aff<Runtime, HttpResponseMessage> PutState(PutStateRequest request, string jwt,
        string connectionId)
    {
        const string spclient = "https://gae2-spclient.spotify.com:443";
        var putstateUrl = $"{spclient}/connect-state/v1/devices/{request.Device.DeviceInfo.DeviceId}";

        var authHeader = new AuthenticationHeaderValue("Bearer", jwt);
        var headers = new HashMap<string, string>();
        headers = headers.Add("X-Spotify-Connection-Id", connectionId);

        var bytes = request.ToByteArray();
        using var ms = new MemoryStream();
        using (var gzip = new GZipStream(ms, CompressionMode.Compress, true))
        {
            gzip.Write(bytes, 0, bytes.Length);
        }

        ms.Position = 0;

        var content = new ByteArrayContent(ms.ToArray());
        content.Headers.ContentType = new MediaTypeHeaderValue("application/protobuf");
        content.Headers.ContentEncoding.Add("gzip");

        return
            from msg in Http<Runtime>.Put(putstateUrl, content, headers, authHeader)
                .Map(x => x.EnsureSuccessStatusCode())
            select msg;
    }
}

public interface ISpotifyRemote
{
    Option<string> ConnectionId { get; }

    ValueTask Connect();

    IObservable<Option<string>> OnConnectionIdChanged { get; }
}