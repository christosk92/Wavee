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
using Wavee.Infrastructure.Live;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Sys.Connection;
using Wavee.Spotify.Sys.Connection.Contracts;
using Wavee.Spotify.Sys.Crypto;
using Wavee.Spotify.Sys.Tokens;

namespace Wavee.Spotify.Sys.Remote;

public static class SpotifyRemote
{
    public static async ValueTask<SpotifyRemoteInfo> Connect(
        this SpotifyConnectionInfo connection,
        SpotifyRemoteConfig config,
        CancellationToken ct = default)
    {
        var cluster = await SpotifyRemoteClient<WaveeRuntime>.Connect(
            connection,
            config,
            ct).Run(WaveeCore.Runtime);

        return cluster
            .Match(
                Succ: c => c,
                Fail: e => throw e
            );
    }
}

internal static class SpotifyRemoteClient<RT>
    where RT : struct, HasWebsocket<RT>, HasHttp<RT>
{
    public static Atom<HashMap<Guid, ChannelReader<SpotifyWebsocketMessage>>> ConnectionConsumer =
        Atom(LanguageExt.HashMap<Guid, ChannelReader<SpotifyWebsocketMessage>>.Empty);

    public static Atom<HashMap<Guid, ChannelWriter<SpotifyWebsocketMessage>>> ConnectionProducer =
        Atom(LanguageExt.HashMap<Guid, ChannelWriter<SpotifyWebsocketMessage>>.Empty);


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Aff<RT, SpotifyRemoteInfo> Connect(
        SpotifyConnectionInfo connection,
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
            from __ in WebsocketConnectionListener<RT>.StartListening(remoteInfo.ConnectionId)
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
        let newDeviceState = new SpotifyDeviceState(Config: config, DeviceId: connectionInfo.Deviceid)
        from websocket in Ws<RT>.Connect(finalDealerUrl)
        from message in Ws<RT>.Read(websocket)
        from connectionId in ParseConnectionId(message)
        from initialCluster in Put(connectionId,
            connectionInfo.Deviceid,
            accessToken,
            newDeviceState.BuildPutStateRequest(PutStateReason.NewDevice, None), ct)
        from _ in SuccessEff(remoteInfo.With(newDeviceState))
        select (connectionId, websocket, initialCluster);

    private static Aff<RT, Cluster> Put(
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
        string spotifyConnectionId,
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

internal readonly record struct SpotifyDeviceState(SpotifyRemoteConfig Config, string DeviceId)
{
    public PutStateRequest BuildPutStateRequest(PutStateReason reason, Option<TimeSpan> playerTime)
    {
        return new PutStateRequest
        {
            PutStateReason = reason,
            Device = new Device
            {
                PlayerState = BuildPlayerState(this),
                DeviceInfo = new DeviceInfo
                {
                    CanPlay = true,
                    Volume = (uint)(Config.InitialVolume * ushort.MaxValue),
                    Name = Config.DeviceName,
                    DeviceId = DeviceId,
                    DeviceType = Config.DeviceType,
                    DeviceSoftwareVersion = "1.0.0",
                    ClientId = SpotifyConstants.KEYMASTER_CLIENT_ID,
                    SpircVersion = "3.2.6",
                    Capabilities = new Capabilities
                    {
                        CanBePlayer = true,
                        GaiaEqConnectId = true,
                        SupportsLogout = true,
                        IsObservable = true,
                        CommandAcks = true,
                        SupportsRename = false,
                        SupportsPlaylistV2 = true,
                        IsControllable = true,
                        SupportsTransferCommand = true,
                        SupportsCommandRequest = true,
                        VolumeSteps = (int)64,
                        SupportsGzipPushes = true,
                        NeedsFullPlayerState = false,
                        SupportedTypes = { "audio/episode", "audio/track" }
                    }
                }
            }
        };
    }

    private static PlayerState BuildPlayerState(SpotifyDeviceState deviceState)
    {
        return new PlayerState
        {
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
            PlaybackSpeed = 1.0,
            IsPlaying = false
        };
    }
}

public readonly record struct SpotifyRemoteConfig(
    string DeviceName,
    DeviceType DeviceType,
    float InitialVolume = 0.5f);