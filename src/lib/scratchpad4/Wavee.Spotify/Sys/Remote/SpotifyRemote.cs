using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using CommunityToolkit.HighPerformance;
using Eum.Spotify;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using LanguageExt.UnsafeValueAccess;
using Wavee.Infrastructure.Live;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;
using Wavee.Player;
using Wavee.Spotify.Sys.Connection;
using Wavee.Spotify.Sys.Connection.Contracts;
using Wavee.Spotify.Sys.Crypto;
using Wavee.Spotify.Sys.Playback;
using Wavee.Spotify.Sys.Tokens;

namespace Wavee.Spotify.Sys.Remote;

public static class SpotifyRemote
{
    public static async ValueTask<SpotifyRemoteInfo> Connect(
        this SpotifyConnectionInfo connection,
        IWaveePlayer player,
        SpotifyRemoteConfig config,
        CancellationToken ct = default)
    {
        var cluster = await SpotifyRemoteClient<WaveeRuntime>.Connect(
            connection,
            player,
            config,
            ct).Run(WaveeCore.Runtime);
        var remoteInfo = cluster.ThrowIfFail();

        player.CurrentItemChanged.Subscribe(async c =>
        {
            if (c.IsSome)
            {
                var item = c.ValueUnsafe();
                var state = atomic(() => remoteInfo.DeviceStateRef.Swap(c =>
                {
                    return c.Match(
                        None: () => Option<SpotifyDeviceState>.None,
                        Some: state => state.WithTrack(item)
                    );
                }));

                if (state.IsSome)
                {
                    var jwt = await connection.FetchAccessToken();
                    var putAff = await SpotifyRemoteClient<WaveeRuntime>.Put(
                        remoteInfo.SpotifyConnectionIdRef.Value.ValueUnsafe(),
                        connection.Deviceid,
                        jwt,
                        state.ValueUnsafe()
                            .BuildPutStateRequest(PutStateReason.PlayerStateChanged, player.CurrentPosition),
                        CancellationToken.None
                    ).Run(WaveeCore.Runtime);
                    var cl = putAff.ThrowIfFail();
                    
                }
            }
        });
        player.PlayContextChanged.Subscribe(c =>
        {
            if (c.IsSome)
            {
                var item = c.ValueUnsafe();
            }
        });
        player.IsPausedChanged.Subscribe(c => { });
        player.CurrentPositionChanged.Subscribe(c => { });

        return remoteInfo;
    }
}

internal static class SpotifyRemoteClient<RT>
    where RT : struct, HasWebsocket<RT>, HasHttp<RT>, HasTCP<RT>
{
    public static Atom<HashMap<Guid, ChannelReader<SpotifyWebsocketMessage>>> ConnectionConsumer =
        Atom(LanguageExt.HashMap<Guid, ChannelReader<SpotifyWebsocketMessage>>.Empty);

    public static Atom<HashMap<Guid, ChannelWriter<SpotifyWebsocketMessage>>> ConnectionProducer =
        Atom(LanguageExt.HashMap<Guid, ChannelWriter<SpotifyWebsocketMessage>>.Empty);


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Aff<RT, SpotifyRemoteInfo> Connect(
        SpotifyConnectionInfo connection,
        IWaveePlayer player,
        SpotifyRemoteConfig config, CancellationToken ct = default)
    {
        //these are not related

        //this one is used from the ws and dispatches them to ConnectionConsumer
        //so ws -> consumer
        var coreChannel = Channel.CreateUnbounded<SpotifyWebsocketMessage>();

        //this one is used to write to the ws connection, and are read from ConnectionProducer
        //so producer -> ws
        var coreWriteChannel = Channel.CreateUnbounded<SpotifyWebsocketMessage>();

        var remoteInfo = new SpotifyRemoteInfo
        {
            ConnectionId = Guid.NewGuid()
        };

        atomic(() =>
        {
            ConnectionConsumer.Swap(connections =>
                connections.Add(remoteInfo.ConnectionId, coreChannel));
            ConnectionProducer.Swap(connections =>
                connections.Add(remoteInfo.ConnectionId, coreWriteChannel));
        });
        ;

        return
            from _ in ConnectAndAuthenticateToWebsocket(
                connectionInfo: connection,
                remoteInfo: remoteInfo,
                writer: coreChannel.Writer,
                reader: coreWriteChannel.Reader,
                config: config,
                ct: ct)
            from __ in Aff<RT, Unit>(async rt =>
            {
                //start consuming messages/requests
                await Task.Factory.StartNew(async () =>
                {
                    var channel = coreChannel.Reader;
                    await foreach (var packet in channel.ReadAllAsync())
                    {
                        //parse message
                        var parsed = SpotifyRemoteMessage.ParseFrom(packet.Data);
                        switch (parsed.Type)
                        {
                            case SpotifyRemoteMessageType.Message:
                                if (parsed.Uri.StartsWith("hm://connect-state/v1/cluster"))
                                {
                                    var clusterUpdate = ClusterUpdate.Parser.ParseFrom(parsed.Payload.Span);
                                    var cluster = clusterUpdate.Cluster;
                                    remoteInfo.With(remoteInfo.SpotifyConnectionIdRef.Value, cluster);
                                }

                                break;
                            case SpotifyRemoteMessageType.Request:
                            {
                                using var jsonDocument = JsonDocument.Parse(parsed.Payload);
                                var request = jsonDocument.RootElement;
                                var messageId = request.GetProperty("message_id").GetInt32();
                                var sentBy = request.GetProperty("sent_by_device_id").GetString();
                                var command = request.GetProperty("command");
                                var endpoint = command.GetProperty("endpoint").GetString() switch
                                {
                                    "transfer" => SpotifyRequestCommandType.Transfer,
                                    _ => throw new ArgumentOutOfRangeException()
                                };
                                ReadOnlyMemory<byte> data = command.GetProperty("data").GetBytesFromBase64();
                                var requestCommand = new SpotifyRequestCommand(
                                    MessageId: messageId,
                                    SentBy: sentBy,
                                    Endpoint: endpoint,
                                    Data: data
                                );
                                atomic(() => remoteInfo.DeviceStateRef
                                    .Swap(f =>
                                    {
                                        return f.Match(
                                            None: () => new SpotifyDeviceState(config,
                                                connection.Deviceid, messageId,
                                                sentBy, Option<PlayOrigin>.None)
                                            {
                                                LastCommandId = messageId,
                                                LastCommandSentByDeviceId = sentBy
                                            },
                                            Some: s => s with
                                            {
                                                LastCommandId = messageId,
                                                LastCommandSentByDeviceId = sentBy
                                            }
                                        );
                                    }));
                                var didSomething = await SpotifyPlaybackRuntime<RT>
                                    .Handle(requestCommand, player, remoteInfo, connection, config)
                                    .Run(rt);

                                bool success = false;
                                if (didSomething.IsSucc)
                                {
                                    var response = didSomething.Match(Succ: r => r, Fail: _ => throw new Exception());
                                    success = response;
                                }

                                var responsePayload = new
                                {
                                    type = "reply",
                                    key = parsed.Uri,
                                    payload = new
                                    {
                                        success = success.ToString().ToLower()
                                    }
                                };
                                ReadOnlyMemory<byte> responsePayloadJson =
                                    JsonSerializer.SerializeToUtf8Bytes(responsePayload);
                                await coreWriteChannel.Writer.WriteAsync(new SpotifyWebsocketMessage
                                {
                                    Data = responsePayloadJson
                                }, ct);
                                break;
                            }
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }, TaskCreationOptions.LongRunning);
                return unit;
            })
            select remoteInfo;
    }


    private static Aff<RT, Unit>
        ConnectAndAuthenticateToWebsocket(
            SpotifyConnectionInfo connectionInfo,
            SpotifyRemoteInfo remoteInfo,
            ChannelWriter<SpotifyWebsocketMessage> writer,
            ChannelReader<SpotifyWebsocketMessage> reader,
            SpotifyRemoteConfig config,
            CancellationToken ct = default)
    {
        return
            from connectionResult in ConnectAndAuthenticate(
                config: config,
                connectionInfo,
                remoteInfo,
                ct)
            let newRemoteInfo = remoteInfo.With(connectionResult.Item1, connectionResult.InitialCluster)
            from _ in StartMessageReader(
                writer: writer,
                reader: reader,
                networkStream: connectionResult.Item2,
                onDisconnection: async (rt, errorMaybe) =>
                {
                    if (errorMaybe.IsNone)
                    {
                        //do nothing
                        return;
                    }

                    var msg = errorMaybe.MatchUnsafe(
                        Some: e => e.Message,
                        None: () => throw new Exception("Impossible")
                    );
                    Debug.WriteLine(msg);
                    bool connected = false;
                    while (!connected)
                    {
                        var run = await OnDisconnection(newRemoteInfo,
                                connectionInfo,
                                writer,
                                reader,
                                config, ct)
                            .Run(rt);
                        connected = run.IsSucc;
                        await Task.Delay(3000, ct);
                    }
                })
            select unit;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Aff<RT, (string ConnectionId, WebSocket socket, Cluster InitialCluster)>
        ConnectAndAuthenticate(
            SpotifyRemoteConfig config,
            SpotifyConnectionInfo connectionInfo,
            SpotifyRemoteInfo remoteInfo,
            CancellationToken ct) =>
        from dealer in AP<RT>.FetchDealer().Map(x => $"wss://{x.Host}:{x.Port}?access_token={{0}}")
        from accessToken in connectionInfo.FetchAccessToken().ToAff()
        let finalDealerUrl = string.Format(dealer, accessToken)
        let newDeviceState = new SpotifyDeviceState(Config: config, DeviceId: connectionInfo.Deviceid, Option<int>.None,
            None, None)
        from websocket in Ws<RT>.Connect(finalDealerUrl)
        from message in Ws<RT>.Read(websocket)
        from connectionId in ParseConnectionId(message)
        from initialCluster in Put(connectionId,
            connectionInfo.Deviceid,
            accessToken,
            newDeviceState.BuildPutStateRequest(PutStateReason.NewDevice, None), ct)
        from _ in SuccessEff(remoteInfo.With(newDeviceState))
        select (connectionId, websocket, initialCluster);

    internal static Aff<RT, Cluster> Put(
        string connectionId,
        string deviceId,
        string accessToken,
        PutStateRequest putstate,
        CancellationToken ct) =>
        from body in Eff(() =>
        {
            //gzip the body

            ReadOnlyMemory<byte> bodyBytes = putstate.ToByteArray().AsMemory();
            var compressed = Compress(bodyBytes.AsStream());
            compressed.Flush();
            compressed.Position = 0;
            var byteArrayContent = new StreamContent(compressed);
            byteArrayContent.Headers.Add("Content-Encoding", "gzip");
            //protobuf content type
            byteArrayContent.Headers.Add("Content-Type", "application/protobuf");
            return byteArrayContent;
        })
        from authHeader in SuccessEff(new AuthenticationHeaderValue("Bearer", accessToken))
        from connectionIdHeaders in SuccessEff(
            new HashMap<string, string>()
                .Add("X-Spotify-Connection-Id", connectionId)
                .Add("accept", "gzip"))
        from url in AP<RT>.FetchSpClient().Map(f => $"https://{f.Host}:{f.Port}/connect-state/v1/devices/{deviceId}")
        from clusterResponse in Http<RT>.Put(url, authHeader, connectionIdHeaders, body)
            .MapAsync(async c =>
            {
                c.EnsureSuccessStatusCode();
                await using var stream = await c.Content.ReadAsStreamAsync(ct);
                await using var decodedGzip = Decompress(stream);
                decodedGzip.Position = 0;
                var cluster = Cluster.Parser.ParseFrom(decodedGzip);
                return cluster;
            })
        select clusterResponse;

    private static MemoryStream Decompress(Stream compressedStream, bool leaveStreamOpen = false)
    {
        if (compressedStream.Position == compressedStream.Length)
        {
            compressedStream.Seek(0, SeekOrigin.Begin);
        }

        var uncompressedStream = new MemoryStream(GetGzipUncompressedLength(compressedStream));
        using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, leaveStreamOpen))
        {
            gzipStream.CopyTo(uncompressedStream);
        }

        return uncompressedStream;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetGzipUncompressedLength(Stream stream)
    {
        Span<byte> uncompressedLength = stackalloc byte[4];
        stream.Position = stream.Length - 4;
        stream.Read(uncompressedLength);
        stream.Seek(0, SeekOrigin.Begin);
        return BitConverter.ToInt32(uncompressedLength);
    }

    private static MemoryStream Compress(Stream inputStream, bool leaveStreamOpen = false)
    {
        if (inputStream.Position == inputStream.Length)
        {
            inputStream.Seek(0, SeekOrigin.Begin);
        }

        var compressedStream = new MemoryStream();
        using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.SmallestSize, true))
        {
            inputStream.CopyTo(gzipStream);
            gzipStream.Flush();
        }

        if (!leaveStreamOpen)
        {
            inputStream.Close();
        }

        compressedStream.Seek(0, SeekOrigin.Begin);
        return compressedStream;
    }
    // private static ArraySegment<byte> Decompress(ReadOnlyMemory<byte> compressedData)
    // {
    //     using var uncompressedStream = new MemoryStream();
    //     using (var gzipStream = new GZipStream(compressedData.AsStream(), CompressionMode.Decompress, false))
    //     {
    //         gzipStream.CopyTo(uncompressedStream);
    //     }
    //
    //     if (uncompressedStream.TryGetBuffer(out var buffer))
    //     {
    //         return buffer;
    //     }
    //     else
    //     {
    //         return uncompressedStream.ToArray();
    //     }
    // }

    private static Eff<string> ParseConnectionId(ReadOnlyMemory<byte> receivedMessage)
    {
        return Eff(() =>
        {
            using var reader = JsonDocument.Parse(receivedMessage);
            var connectionid = reader.RootElement.GetProperty("headers")
                .GetProperty("Spotify-Connection-Id")
                .GetString();
            return connectionid!;
        });
    }

    private static Aff<RT, Unit> StartMessageReader(
        ChannelWriter<SpotifyWebsocketMessage> writer,
        ChannelReader<SpotifyWebsocketMessage> reader,
        WebSocket networkStream,
        Action<RT, Option<Error>> onDisconnection)
    {
        return Aff<RT, Unit>(async rt =>
        {
            await Task.Factory.StartNew(async () =>
            {
                await ReadMessageRecursive(
                    runtime: rt,
                    writer: writer,
                    networkStream: networkStream,
                    onDisconnection: onDisconnection
                );
            }, TaskCreationOptions.LongRunning);

            await Task.Factory.StartNew(async () =>
            {
                await WriteMessageRecursive(
                    runtime: rt,
                    reader: reader,
                    networkStream: networkStream,
                    onDisconnection: onDisconnection
                );
            }, TaskCreationOptions.LongRunning);
            return unit;
        });
    }

    private static async Task<Unit> WriteMessageRecursive(
        RT runtime,
        ChannelReader<SpotifyWebsocketMessage> reader,
        WebSocket networkStream,
        Action<RT, Option<Error>> onDisconnection)
    {
        var run =
            await WriteMessage(
                reader,
                networkStream).Run(runtime);

        if (run.IsFail)
        {
            var error = run.Match(Succ: _ => throw new Exception("Impossible"), Fail: e => e);
            onDisconnection(runtime, error);
            return unit;
        }

        var newRecord = run.Match(Succ: e => e, Fail: _ => throw new Exception("Impossible"));

        //call self
        return await WriteMessageRecursive(
            runtime,
            reader,
            networkStream,
            onDisconnection);
    }

    private static async Task<Unit> ReadMessageRecursive(
        RT runtime,
        ChannelWriter<SpotifyWebsocketMessage> writer,
        WebSocket networkStream,
        Action<RT, Option<Error>> onDisconnection)
    {
        var run =
            await ReadMessage(
                writer,
                networkStream).Run(runtime);

        if (run.IsFail)
        {
            var error = run.Match(Succ: _ => throw new Exception("Impossible"), Fail: e => e);
            onDisconnection(runtime, error);
            return unit;
        }

        var newRecord = run.Match(Succ: e => e, Fail: _ => throw new Exception("Impossible"));

        //call self
        return await ReadMessageRecursive(
            runtime,
            writer,
            networkStream,
            onDisconnection);
    }


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Aff<RT, Unit> WriteMessage(
        ChannelReader<SpotifyWebsocketMessage> writer,
        WebSocket networkStream) =>
        from _ in writer.WaitToReadAsync().ToAff()
        from message in writer.ReadAsync().ToAff()
        from sent in Ws<RT>
            .Write(networkStream, message.Data)
        select unit;


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Aff<RT, Unit> ReadMessage(
        ChannelWriter<SpotifyWebsocketMessage> writer,
        WebSocket networkStream) =>
        from message in Ws<RT>
            .Read(networkStream)
        from dispatch in Eff(() =>
        {
            if (writer.TryWrite(new SpotifyWebsocketMessage(message)))
                return unit;
            throw new Exception("Failed to write to channel. Probably closed.");
        })
        select unit;


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Aff<RT, Unit> OnDisconnection(SpotifyRemoteInfo remoteInfo,
        SpotifyConnectionInfo connectionInfo,
        ChannelWriter<SpotifyWebsocketMessage> writer,
        ChannelReader<SpotifyWebsocketMessage> reader,
        SpotifyRemoteConfig config, CancellationToken ct) =>
        from __ in SuccessEff(atomic(() => connectionInfo.With(None)))
        from _ in ConnectAndAuthenticateToWebsocket(connectionInfo, remoteInfo, writer, reader, config, ct)
        select unit;
}

internal readonly record struct SpotifyWebsocketMessage(ReadOnlyMemory<byte> Data);

public class SpotifyRemoteInfo
{
    internal LanguageExt.Ref<Option<SpotifyDeviceState>> DeviceStateRef = Ref(Option<SpotifyDeviceState>.None);
    internal LanguageExt.Ref<Option<Cluster>> ClusterRef = Ref(Option<Cluster>.None);
    internal LanguageExt.Ref<Option<string>> SpotifyConnectionIdRef = Ref(Option<string>.None);

    public required Guid ConnectionId { get; init; }

    public Option<Cluster> Cluster => ClusterRef.Value;

    public IObservable<Option<Cluster>> ClusterChanged => ClusterRef.OnChange();

    internal SpotifyRemoteInfo With(
        Option<string> spotifyConnectionId,
        Cluster cluster)
    {
        atomic(() =>
        {
            SpotifyConnectionIdRef.Swap(f => spotifyConnectionId);
            ClusterRef.Swap(f => cluster);
        });
        return this;
    }

    internal SpotifyRemoteInfo With(SpotifyDeviceState deviceState)
    {
        atomic(() => { DeviceStateRef.Swap(f => deviceState); });
        return this;
    }
}

public readonly record struct SpotifyRemoteConfig(
    string DeviceName,
    DeviceType DeviceType,
    PreferredQualityType PreferredQuality,
    float InitialVolume = 0.5f);