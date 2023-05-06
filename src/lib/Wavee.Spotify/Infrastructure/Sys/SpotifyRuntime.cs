using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Eum.Spotify;
using LanguageExt.Effects.Traits;
using LanguageExt.UnsafeValueAccess;
using Wavee.Infrastructure.Live;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;
using Wavee.Player;
using Wavee.Spotify.Contracts;
using Wavee.Spotify.Infrastructure.ApResolver;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Spotify.Infrastructure.Crypto;
using Wavee.Spotify.Remote.Infrastructure.Sys;

namespace Wavee.Spotify.Infrastructure.Sys;

public static class SpotifyRuntime
{
    private static Ref<HashMap<Guid, ConnectionController>> Connections { get; } =
        Ref(LanguageExt.HashMap<Guid, ConnectionController>.Empty);

    internal static Unit RemoveListener(Guid connectionId, ChannelReader<Either<Error, SpotifyPacket>> readerRef)
    {
        atomic(() => Connections.Swap(oldConns =>
        {
            return oldConns.Find(connectionId).Match(
                Some: controller =>
                {
                    var newSeq = controller.Reader.Filter(x => x.Reader != readerRef);
                    return oldConns.AddOrUpdate(connectionId, controller with { Reader = newSeq });
                },
                None: () => throw new KeyNotFoundException("Connection not found"));
        }));

        return unit;
    }

    internal static ChannelReader<Either<Error, SpotifyPacket>> GetChannelReader(Guid connectionId)
    {
        var connections = Connections;
        var newReader = Channel.CreateUnbounded<Either<Error, SpotifyPacket>>();
        atomic(() =>
        {
            connections.Swap(oldConns =>
            {
                return oldConns.AddOrUpdate(connectionId,
                    Some: controller => controller with { Reader = controller.Reader.Add(newReader) },
                    None: () =>
                    {
                        var newChannel = Channel.CreateUnbounded<SpotifyPacket>();
                        return new ConnectionController(newChannel, new Seq<Channel<Either<Error, SpotifyPacket>>>(new[]
                        {
                            newReader
                        }));
                    });
            });
        });

        return newReader.Reader;
    }


    internal static ChannelWriter<SpotifyPacket> GetSender(Guid connectionId)
    {
        var writer = Connections.Value.Find(connectionId).Match(
            Some: controller => controller.Sender,
            None: () => throw new KeyNotFoundException("Connection not found"));
        return writer;
    }

    public static ISpotifyClient Create()
    {
        var connectionId = Guid.NewGuid();
        var newClient = new SpotifyClient<WaveeRuntime>(connectionId, WaveeCore.Runtime);
        return newClient;
    }

    /// <summary>
    /// Create a new connection to Spotify.
    /// </summary>
    /// <param name="credentials">
    /// The credentials to use for authentication.
    /// </param>
    /// <param name="autoReconnect">
    /// Whether to automatically reconnect if the connection is lost.
    /// </param>
    /// <returns>
    /// A new <see cref="ISpotifyClient"/> instance.
    /// </returns>
    public static async ValueTask<ISpotifyClient> Authenticate(
        IWaveePlayer player,
        SpotifyConfig config,
        Option<ISpotifyClient> existingClient,
        LoginCredentials credentials,
        bool autoReconnect = true)
    {
        var (connectionId, newClient) = existingClient.BiMap(
                Some: client => (client.ConnectionId, client as ISpotifyClient),
                None: () =>
                {
                    var connectionId = Guid.NewGuid();
                    return (connectionId, new SpotifyClient<WaveeRuntime>(connectionId, WaveeCore.Runtime));
                })
            .ValueUnsafe();
        var deviceId = Guid.NewGuid().ToString();
        var result =
            await (
                    from apWelcome in Authenticate<WaveeRuntime>(
                        deviceId,
                        newClient, credentials, autoReconnect,
                        connectionId,
                        Connections)
                    from lastClient in Eff(() =>
                    {
                        return newClient switch
                        {
                            SpotifyClient<WaveeRuntime> r => r.OnApWelcome(apWelcome),
                            _ => newClient
                        };
                    })
                    from spClientUrl in AP<WaveeRuntime>.FetchSpClient()
                        .Map(c => $"https://{c.Host}:{c.Port}")
                    from remoteState in SpotifyRemoteRuntime<WaveeRuntime>.Connect(
                        lastClient,
                        player,
                        deviceId,
                        config.DeviceName,
                        config.DeviceType,
                        spClientUrl,
                        newClient.Mercury.FetchBearer().AsTask)
                    select (lastClient, remoteState)
                )
                .Run(WaveeCore.Runtime);

        return result.Match(
            Succ: welcome =>
            {
                return welcome.lastClient switch
                {
                    SpotifyClient<WaveeRuntime> r => r.OnRemote(welcome.remoteState),
                    _ => welcome.lastClient
                };
            },
            Fail: e => throw e);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Aff<RT, APWelcome> Authenticate<RT>(
        string deviceId,
        ISpotifyClient client,
        LoginCredentials credentials,
        bool autoCorrect,
        Guid connectionId,
        Ref<HashMap<Guid, ConnectionController>> connections)
        where RT : struct, HasHttp<RT>, HasTCP<RT>, HasWebsocket<RT> =>
        from ct in cancelToken<RT>()
        let connectionController = atomic(() => connections.Swap(k =>
        {
            return k.AddOrUpdate(connectionId, Some: controller => controller, None: () =>
            {
                var channel = Channel.CreateUnbounded<SpotifyPacket>();
                return new ConnectionController(channel, LanguageExt.Seq<Channel<Either<Error, SpotifyPacket>>>.Empty);
            });
        }))
        from channel in connectionController.Find(connectionId).ToEff().Map(x => x.Sender)
        from welcomeMessage in AuthenticateWithTcp<RT>(client, channel.Reader, credentials, connectionId,
            deviceId,
            autoCorrect, ct)
        select welcomeMessage;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Aff<RT, APWelcome> AuthenticateWithTcp<RT>(
        ISpotifyClient client,
        ChannelReader<SpotifyPacket> reader, LoginCredentials credentials,
        Guid connectionId,
        string deviceId,
        bool autoCorrect, CancellationToken ct)
        where RT : struct, HasHttp<RT>, HasTCP<RT>, HasWebsocket<RT> =>
        ConnectAndAuthenticate<RT>(credentials, deviceId)
            .Bind(p =>
                HandleDisconnection<RT>(client, reader, credentials, connectionId, deviceId, autoCorrect,
                        ct, p.Item2, p.Item3)
                    .Map(_ => p.Item1));

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Aff<RT, (APWelcome, NetworkStream, SpotifyEncryptionRecord)> ConnectAndAuthenticate<RT>(
        LoginCredentials credentials, string deviceId)
        where RT : struct, HasHttp<RT>, HasTCP<RT> =>
        from hostPortResponse in AP<RT>.FetchHostAndPort()
        from tcpClient in Tcp<RT>.Connect(hostPortResponse.Host, hostPortResponse.Port)
        let stream = tcpClient.GetStream()
        from clientHelloResult in Handshake<RT>.PerformClientHello(stream)
        from nonceAfterAuthAndApWelcome in Authentication<RT>.Authenticate(stream, clientHelloResult, credentials,
            deviceId)
        select (nonceAfterAuthAndApWelcome.ApWelcome, stream, nonceAfterAuthAndApWelcome.EncryptionRecord);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Aff<RT, Unit> HandleDisconnection<RT>(
        ISpotifyClient client,
        ChannelReader<SpotifyPacket> reader,
        LoginCredentials credentials,
        Guid connectionId,
        string deviceId,
        bool autoCorrect,
        CancellationToken ct,
        NetworkStream stream,
        SpotifyEncryptionRecord encryptionRecord)
        where RT : struct, HasHttp<RT>, HasTCP<RT>, HasWebsocket<RT> =>
        Eff<RT, Unit>((r) =>
        {
            Task.Run(async () =>
            {
                var d = await ProcessMessages<RT>(connectionId, reader, stream, Ref(encryptionRecord)).Run(r);
                _ = d.Match(
                    Fail: async e =>
                    {
                        if (autoCorrect)
                        {
                            await Reconnect<RT>(client, reader, credentials, connectionId, deviceId, ct,
                                r);
                        }
                        else
                        {
                            throw e;
                        }
                    },
                    Succ: _ => { });
            });
            return unit;
        });

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static async Task<Unit> Reconnect<RT>(
        ISpotifyClient client,
        ChannelReader<SpotifyPacket> reader,
        LoginCredentials credentials,
        Guid connectionId,
        string deviceId,
        CancellationToken ct,
        RT runtime)
        where RT : struct, HasHttp<RT>, HasTCP<RT>, HasWebsocket<RT>
    {
        bool connected = false;
        while (!connected)
        {
            var result =
                await AuthenticateWithTcp<RT>(client, reader, credentials, connectionId, deviceId, true,
                    ct).Run(runtime);
            if (result.IsSucc)
            {
                connected = true;
                var welcome = result.Match(
                    Succ: w => w,
                    Fail: e => throw e);

                if (client is SpotifyClient<RT> cl)
                {
                    cl.OnApWelcome(welcome);
                }
            }
            else
            {
                //retry
                connected = false;
                await Task.Delay(5000, ct);
            }
        }

        return unit;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Aff<RT, Unit> ProcessMessages<RT>(
        Guid connectionId,
        ChannelReader<SpotifyPacket> channel, NetworkStream stream,
        Ref<SpotifyEncryptionRecord> encryptionRecord)
        where RT : struct, HasCancel<RT>, HasTCP<RT>
    {
        return Aff<RT, Unit>(async env =>
        {
            using var cts = new CancellationTokenSource();
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, env.CancellationToken);
            // Continuously read messages from the channel, encrypt them, and send them over the TCP connection
            var sendingTask = Task.Run(async () =>
            {
                await foreach (var message in channel.ReadAllAsync(combinedCts.Token))
                {
                    // Perform the encryption
                    var encryptedMessage = await Authentication<RT>
                        .SendEncryptedMessage(stream, message, encryptionRecord)
                        .Run(env);
                    encryptedMessage.Match(
                        Succ: r => { atomic(() => encryptionRecord.Swap(k => r)); },
                        Fail: e => throw e);
                }
            }, combinedCts.Token);

            // Continuously listen for messages and decrypt them
            var listeningTask = ReadAndProcessMessage<RT>(connectionId, stream, encryptionRecord)
                .Run(env).AsTask();
            var listeningTaskResult = await listeningTask;
            await combinedCts.CancelAsync();
            return listeningTaskResult.Match(
                Succ: _ => unit,
                Fail: e => throw e);
        });
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Aff<RT, Unit> ReadAndProcessMessage<RT>(
        Guid connectionId,
        NetworkStream stream,
        SpotifyEncryptionRecord encryptionRecord)
        where RT : struct, HasCancel<RT>, HasTCP<RT>
    {
        return Aff<RT, Unit>(async env =>
        {
            while (true)
            {
                var messageResult = await Authentication<RT>
                    .ReadDecryptedMessage(stream, encryptionRecord)
                    .Run(env);
                if (messageResult.IsFail)
                {
                    var err = messageResult.Match(Succ: _ => throw new Exception("Impossible"), Fail: identity);
                    Connections.Value
                        .Find(x => x.Key == connectionId)
                        .IfSome(dispatchers =>
                        {
                            dispatchers.Value.Reader.Iter(dispatcher => { dispatcher.Writer.TryWrite(err); });
                        });
                    throw err;
                    //return unit;
                }

                var msg = messageResult.Match(Succ: identity, Fail: _ => throw new Exception("Impossible"));

                switch (msg.Packet.Command)
                {
                    case SpotifyPacketType.PongAck:
                        Debug.WriteLine("PongAck");
                        break;
                    case SpotifyPacketType.Ping:
                        //handle ourselves
                        Connections.Value
                            .Find(x => x.Key == connectionId)
                            .IfSome(connections =>
                            {
                                var pong = new SpotifyPacket(SpotifyPacketType.Pong, new byte[4]);
                                connections.Value.Sender.Writer.TryWrite(pong);
                            });
                        break;
                    case SpotifyPacketType.ProductInfo or SpotifyPacketType.CountryCode
                        or SpotifyPacketType.MercuryEvent or SpotifyPacketType.MercuryReq
                        or SpotifyPacketType.MercurySub or SpotifyPacketType.MercuryUnsub
                        or SpotifyPacketType.AesKey or SpotifyPacketType.AesKeyError:
                        Connections.Value
                            .Find(x => x.Key == connectionId)
                            .IfSome(dispatchers =>
                            {
                                dispatchers.Value.Reader.Iter(dispatcher =>
                                {
                                    dispatcher.Writer.TryWrite(msg.Packet);
                                });
                            });
                        break;
                    default:
                        Debug.WriteLine($"Unhandled packet type: {msg.Packet.Command}");
                        break;
                }

                encryptionRecord = msg.NewEncryptionRecord;
            }
        });
    }
}

/// <summary>
/// A connection controller is used to control a traffic between Spotify.
/// </summary>
/// <param name="Sender">
/// The channel writer used to send packets to Spotify. Use this to send unencrypted packets to Spotify, will be encrypted automatically.
/// </param>
/// <param name="Reader">
/// The channel reader used to read packets from Spotify. Use this to read decrypted packets from Spotify.
/// </param>
internal readonly record struct ConnectionController(Channel<SpotifyPacket> Sender,
    Seq<Channel<Either<Error, SpotifyPacket>>> Reader);