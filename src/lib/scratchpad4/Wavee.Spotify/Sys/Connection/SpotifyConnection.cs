using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Eum.Spotify;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Sys.Connection;
using Wavee.Spotify.Sys.Connection.Contracts;
using Wavee.Spotify.Sys.Crypto;
using SpotifyPacket = Wavee.Spotify.Sys.Connection.Contracts.SpotifyPacket;

namespace Wavee.Spotify.Sys;

internal static class SpotifyConnection<RT> where RT : struct,
    HasTCP<RT>, HasHttp<RT>
{
    public static Atom<HashMap<Guid, ChannelReader<SpotifyPacket>>> ConnectionConsumer =
        Atom(LanguageExt.HashMap<Guid, ChannelReader<SpotifyPacket>>.Empty);

    public static Atom<HashMap<Guid, ChannelWriter<SpotifyPacket>>> ConnectionProducer =
        Atom(LanguageExt.HashMap<Guid, ChannelWriter<SpotifyPacket>>.Empty);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Aff<RT, SpotifyConnectionInfo> Authenticate(
        string deviceId,
        LoginCredentials credentials,
        CancellationToken cancellationToken = default)
    {
        //these are not related

        //this one is used from the tcp connection and dispatches them to ConnectionConsumer
        //so tcp -> consumer
        var coreChannel = Channel.CreateUnbounded<SpotifyPacket>();

        //this one is used to write to the tcp connection, and are read from ConnectionProducer
        //so producer -> tcp
        var coreWriteChannel = Channel.CreateUnbounded<SpotifyPacket>();
        var connectionId = Guid.NewGuid();

        atomic(() =>
        {
            ConnectionConsumer.Swap(connections =>
                connections.Add(connectionId, coreChannel));
            ConnectionProducer.Swap(connections =>
                connections.Add(connectionId, coreWriteChannel));
        });
        var newInfo = new SpotifyConnectionInfo
        {
            ConnectionId = connectionId
        };

        return
            from info in AuthenticateWithoutConnectionId(
                connectionInfo: newInfo,
                writer: coreChannel.Writer,
                reader: coreWriteChannel.Reader,
                credentials: credentials,
                deviceId: deviceId,
                ct: cancellationToken)
            from _ in ConnectionListener<RT>.StartListening(connectionId)
            from __ in StartPingListener(connectionId)
            select info;
    }

    private static Aff<RT, Unit> StartPingListener(Guid connectionId)
    {
        return Aff<RT, Unit>(async rt =>
        {
            await Task.Factory.StartNew(() =>
            {
                Task.Run(async () =>
                {
                    var pingPongAff =
                        from pingOrPongAck in ConnectionListener<RT>.ConsumePacket(connectionId,
                            p => p.Command is SpotifyPacketType.Ping or SpotifyPacketType.PongAck,
                            static () => true)
                        from _ in pingOrPongAck.Command switch
                        {
                            SpotifyPacketType.Ping => HandlePing(connectionId, pingOrPongAck.Data),
                            SpotifyPacketType.PongAck => HandlePongAck(connectionId),
                            _ => throw new Exception("Invalid packet type")
                        }
                        select unit;

                    var run = await pingPongAff.Repeat().Run(rt);
                });
            });
            return unit;
        });
    }

    private static Eff<RT, Unit> HandlePongAck(Guid connectionId)
    {
        Debug.WriteLine("PongAck received for connection {0}", connectionId);
        return unitEff;
    }

    private static Aff<RT, Unit> HandlePing(Guid connectionId, ReadOnlyMemory<byte> data)
    {
        Debug.WriteLine("Ping received for connection {0}", connectionId);
        return Send(connectionId,
            new SpotifyPacket(SpotifyPacketType.Pong, new byte[4]));
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Aff<RT, Unit> Send(Guid connectionId, SpotifyPacket packet) => Aff<RT, Unit>(async _ =>
    {
        var connection = ConnectionProducer.Value.Find(connectionId);
        if (connection.IsNone)
        {
            throw new Exception("Connection not found");
        }

        await connection.ValueUnsafe().WriteAsync(packet);
        return unit;
    });

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Aff<RT, (APWelcome, NetworkStream, SpotifyEncryptionRecord)> ConnectAndAuthenticate(
        LoginCredentials credentials, string deviceId, CancellationToken ct) =>
        from hostPortResponse in AP<RT>.FetchAP()
        from tcpClient in Tcp<RT>.Connect(hostPortResponse.Host, hostPortResponse.Port)
        let stream = tcpClient.GetStream()
        from clientHelloResult in Handshake<RT>.PerformClientHello(stream)
        from nonceAfterAuthAndApWelcome in Authentication<RT>.Authenticate(stream, clientHelloResult, credentials,
            deviceId)
        select (nonceAfterAuthAndApWelcome.ApWelcome, stream, nonceAfterAuthAndApWelcome.EncryptionRecord);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Aff<RT, SpotifyConnectionInfo> AuthenticateWithoutConnectionId(
        SpotifyConnectionInfo connectionInfo,
        ChannelWriter<SpotifyPacket> writer,
        ChannelReader<SpotifyPacket> reader,
        LoginCredentials credentials,
        string deviceId,
        CancellationToken ct)
    {
        // bool handledDisconnection = false;
        // object disconnectionLock = new object();
        return
            from connectionResult in ConnectAndAuthenticate(credentials, deviceId, ct)
            let newConnectionInfo = connectionInfo.With(connectionResult.Item1)
            from _ in StartMessageReader(
                connectionId: newConnectionInfo.ConnectionId,
                writer: writer,
                reader: reader,
                welcomeMessage: connectionResult.Item1,
                networkStream: connectionResult.Item2,
                spotifyEncryptionRecord: connectionResult.Item3,
                onDisconnection: async (rt, errorMaybe) =>
                {
                    // lock (disconnectionLock)
                    // {
                    //     if (handledDisconnection)
                    //         return;
                    //     handledDisconnection = true;
                    // }

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
                        var run = await OnDisconnection(newConnectionInfo, writer, reader, credentials, deviceId, ct)
                            .Run(rt);
                        connected = run.IsSucc;
                        await Task.Delay(3000, ct);
                    }
                })
            select newConnectionInfo.With(connectionResult.Item1);
    }

    private static Aff<RT, Unit> StartMessageReader(Guid connectionId,
        ChannelWriter<SpotifyPacket> writer,
        ChannelReader<SpotifyPacket> reader,
        APWelcome welcomeMessage,
        NetworkStream networkStream,
        SpotifyEncryptionRecord spotifyEncryptionRecord,
        Action<RT, Option<Error>> onDisconnection)
    {
        return Aff<RT, Unit>(async rt =>
        {
            await Task.Factory.StartNew(async () =>
            {
                await ReadMessageRecursive(
                    runtime: rt,
                    connectionId: connectionId,
                    writer: writer,
                    welcomeMessage: welcomeMessage,
                    networkStream: networkStream,
                    spotifyEncryptionRecord: spotifyEncryptionRecord,
                    onDisconnection: onDisconnection
                );
            }, TaskCreationOptions.LongRunning);

            await Task.Factory.StartNew(async () =>
            {
                await WriteMessageRecursive(
                    runtime: rt,
                    connectionId: connectionId,
                    reader: reader,
                    welcomeMessage: welcomeMessage,
                    networkStream: networkStream,
                    spotifyEncryptionRecord: spotifyEncryptionRecord,
                    onDisconnection: onDisconnection
                );
            }, TaskCreationOptions.LongRunning);
            return unit;
        });
    }

    private static async Task<Unit> WriteMessageRecursive(
        RT runtime,
        Guid connectionId,
        ChannelReader<SpotifyPacket> reader,
        APWelcome welcomeMessage,
        NetworkStream networkStream,
        SpotifyEncryptionRecord spotifyEncryptionRecord,
        Action<RT, Option<Error>> onDisconnection)
    {
        var run =
            await WriteMessage(
                reader,
                networkStream,
                spotifyEncryptionRecord).Run(runtime);

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
            connectionId,
            reader,
            welcomeMessage,
            networkStream,
            newRecord,
            onDisconnection);
    }

    private static async Task<Unit> ReadMessageRecursive(
        RT runtime,
        Guid connectionId,
        ChannelWriter<SpotifyPacket> writer,
        APWelcome welcomeMessage,
        NetworkStream networkStream,
        SpotifyEncryptionRecord spotifyEncryptionRecord,
        Action<RT, Option<Error>> onDisconnection)
    {
        var run =
            await ReadMessage(
                writer,
                networkStream,
                spotifyEncryptionRecord).Run(runtime);

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
            connectionId,
            writer,
            welcomeMessage,
            networkStream,
            newRecord,
            onDisconnection);
    }


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Aff<RT, SpotifyEncryptionRecord> WriteMessage(
        ChannelReader<SpotifyPacket> writer,
        NetworkStream networkStream,
        SpotifyEncryptionRecord spotifyEncryptionRecord) =>
        from _ in writer.WaitToReadAsync().ToAff()
        from message in writer.ReadAsync().ToAff()
        from sent in Authentication<RT>
            .SendEncryptedMessage(networkStream, message, spotifyEncryptionRecord)
        select sent;


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Aff<RT, SpotifyEncryptionRecord> ReadMessage(
        ChannelWriter<SpotifyPacket> writer,
        NetworkStream networkStream,
        SpotifyEncryptionRecord spotifyEncryptionRecord) =>
        from message in Authentication<RT>
            .ReadDecryptedMessage(networkStream, spotifyEncryptionRecord)
        from dispatch in Eff(() =>
        {
            if (writer.TryWrite(message.Packet))
                return unit;
            throw new Exception("Failed to write to channel. Probably closed.");
        })
        select message.NewEncryptionRecord;


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Aff<RT, Unit> OnDisconnection(SpotifyConnectionInfo connectionInfo,
        ChannelWriter<SpotifyPacket> writer,
        ChannelReader<SpotifyPacket> reader,
        LoginCredentials credentials, string deviceId, CancellationToken ct) =>
        from _ in AuthenticateWithoutConnectionId(connectionInfo, writer, reader, credentials, deviceId, ct)
        select unit;
}

public record SpotifyConnectionInfo
{
    private readonly Ref<Option<APWelcome>> _welcomeMessage = Ref(Option<APWelcome>.None);
    public required Guid ConnectionId { get; init; }
    public Option<APWelcome> Welcome => _welcomeMessage.Value;
    public IObservable<Option<APWelcome>> WelcomeChanged => _welcomeMessage.OnChange();

    internal SpotifyConnectionInfo With(APWelcome w)
    {
        atomic(() => _welcomeMessage.Swap(_ => w));
        return this;
    }
}