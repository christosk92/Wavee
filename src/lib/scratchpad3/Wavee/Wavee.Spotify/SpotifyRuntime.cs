using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Channels;
using Eum.Spotify;
using LanguageExt.Effects.Traits;
using Wavee.Infrastructure.Live;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.ApResolver;
using Wavee.Spotify.Connection;
using Wavee.Spotify.Crypto;

namespace Wavee.Spotify;

public static class SpotifyRuntime
{
    /// <summary>
    /// Used internally to store the tcp connections (raw encrypted packets)
    /// </summary>
    internal static readonly Ref<HashMap<Guid, ChannelWriter<SpotifyPacket>>> Connections =
        Ref(HashMap<Guid, ChannelWriter<SpotifyPacket>>());

    /// <summary> 
    /// Should be used to consume packets. These will be decrypted and decompressed.
    /// </summary>
    internal static readonly Ref<HashMap<Guid, Seq<(Guid ListenerId, ChannelReader<SpotifyPacket> Listener)>>>
        PacketReaders =
            Ref(HashMap<Guid, Seq<(Guid ListenerId, ChannelReader<SpotifyPacket> Listener)>>());

    /// <summary>
    /// Used internally to dispatch packets to the correct <see cref="PacketReaders"/>
    /// </summary>
    private static readonly Ref<HashMap<Guid, Seq<(Guid ListenerId, ChannelWriter<SpotifyPacket> Listener)>>>
        PacketDispatchers =
            Ref(HashMap<Guid, Seq<(Guid, ChannelWriter<SpotifyPacket>)>>());

    public static async ValueTask<APWelcome> Authenticate(
        LoginCredentials credentials,
        Option<ISpotifyListener> listener)
    {
        const string apResolve = "https://apresolve.spotify.com/?type=accesspoint&type=dealer&type=spclient";

        var result = await Authenticate<WaveeRuntime>(apResolve,
            credentials, listener, Connections, PacketDispatchers).Run(WaveeCore.Runtime);

        return result.Match(
            Succ: t => t,
            Fail: e => throw e);
    }

    internal static Eff<RT, (Guid ListenerId, ChannelReader<SpotifyPacket> Listener)>
        SetupListener<RT>(Guid connectionId) where RT : struct
    {
        var newListenerId = Guid.NewGuid();
        var listener = Channel.CreateUnbounded<SpotifyPacket>();
        return Eff<RT, (Guid ListenerId, ChannelReader<SpotifyPacket> Listener)>((_) =>
        {
            atomic(() =>
            {
                PacketReaders.Swap(x => x.AddOrUpdate(connectionId,
                    Some: r => r.Add((newListenerId, listener.Reader)),
                    None: () => Seq(new[] { (newListenerId, listener.Reader) })));
            });

            atomic(() =>
            {
                PacketDispatchers.Swap(x => x.AddOrUpdate(connectionId,
                    Some: r => r.Add((newListenerId, listener.Writer)),
                    None: () => Seq(new[] { (newListenerId, listener.Writer) })));
            });
            return (newListenerId, listener.Reader);
        });
    }

    internal static Eff<RT, Unit> RemoveListener<RT>(Guid connId, Guid listenerListenerId) where RT : struct =>
        Eff<RT, Unit>((_) =>
        {
            atomic(() =>
            {
                PacketReaders.Swap(x => x.AddOrUpdate(connId,
                    Some: r => r.Filter(x => x.ListenerId != listenerListenerId),
                    None: () => Seq<(Guid ListenerId, ChannelReader<SpotifyPacket> Listener)>()));

                PacketDispatchers.Swap(x => x.AddOrUpdate(connId,
                    Some: r => r.Filter(x => x.ListenerId != listenerListenerId),
                    None: () => Seq<(Guid ListenerId, ChannelWriter<SpotifyPacket> Listener)>()));

                Connections.Swap(x => x.Remove(connId));
            });
            return unit;
        });

    private static Aff<RT, APWelcome> Authenticate<RT>(string apResolve,
        LoginCredentials credentials, Option<ISpotifyListener> listener,
        Ref<HashMap<Guid, ChannelWriter<SpotifyPacket>>> connections,
        Ref<HashMap<Guid, Seq<(Guid ListenerId, ChannelWriter<SpotifyPacket> Listener)>>> packetDispatchers)
        where RT : struct, HasHttp<RT>, HasTCP<RT>
    {
        var connectionId = Guid.NewGuid();
        var deviceId = Guid.NewGuid().ToString();
        var channel = Channel.CreateUnbounded<SpotifyPacket>();
        atomic(() => connections.Swap(x => x.Add(connectionId, channel.Writer)));
        return AuthenticateWithTcp<RT>(apResolve, credentials, listener, channel, deviceId, connectionId,
            packetDispatchers);
    }

    private static Aff<RT, Unit> SendMessage<RT>(ChannelWriter<byte[]> writer, byte[] message)
        where RT : struct, HasCancel<RT>, HasTCP<RT>
        => Aff<RT, Unit>(async (_) =>
        {
            await writer.WriteAsync(message);
            return unit;
        });

    private static Aff<RT, APWelcome> AuthenticateWithTcp<RT>(string apResolve,
        LoginCredentials credentials,
        Option<ISpotifyListener> listener,
        Channel<SpotifyPacket> channel,
        string deviceId, Guid connectionId,
        Ref<HashMap<Guid, Seq<(Guid ListenerId, ChannelWriter<SpotifyPacket> Listener)>>> packetDispatchers)
        where RT : struct, HasHttp<RT>, HasTCP<RT>
    {
        return
            from hostPortResponse in AP<RT>.FetchHostAndPort(apResolve)
            from tcpClient in Tcp<RT>.Connect(hostPortResponse.Host, hostPortResponse.Port)
            let stream = tcpClient.GetStream()
            from clientHelloResult in Handshake<RT>.PerformClientHello(stream)
            from nonceAfterAuthAndApWelcome in Authentication<RT>.Authenticate(stream, clientHelloResult, credentials,
                deviceId)
            from _ in Eff<RT, Unit>((r) =>
            {
                _ = listener.Map(x => x.OnConnected(connectionId));

                Task.Run(() =>
                    ProcessMessages<RT>(
                            connectionId,
                            channel, stream,
                            Ref(nonceAfterAuthAndApWelcome.EncryptionRecord),
                            listener,
                            packetDispatchers)
                        .Run(r));
                return unit;
            })
            select nonceAfterAuthAndApWelcome.ApWelcome;
    }

    internal static Aff<RT, Unit> ProcessMessages<RT>(
        Guid connectionId,
        Channel<SpotifyPacket> channel, NetworkStream stream,
        Ref<SpotifyEncryptionRecord> encryptionRecord,
        Option<ISpotifyListener> spotifyListener,
        Ref<HashMap<Guid, Seq<(Guid ListenerId, ChannelWriter<SpotifyPacket> Listener)>>> packetDispatchers)
        where RT : struct, HasCancel<RT>, HasTCP<RT>
    {
        return Aff<RT, Unit>(async env =>
        {
            // Continuously read messages from the channel, encrypt them, and send them over the TCP connection
            var sendingTask = Task.Run(async () =>
            {
                await foreach (var message in channel.Reader.ReadAllAsync(env.CancellationToken))
                {
                    // Perform the encryption
                    var encryptedMessage = await Authentication<RT>
                        .SendEncryptedMessage(stream, message, encryptionRecord)
                        .Run(env);
                    encryptedMessage.Match(
                        Succ: r => { atomic(() => encryptionRecord.Swap(k => r)); },
                        Fail: e =>
                        {
                            Debug.WriteLine(e);
                            throw e;
                        }
                    );
                }
            });

            // Continuously listen for messages and decrypt them
            var listeningTask = ReadAndProcessMessage<RT>(connectionId, stream, encryptionRecord, spotifyListener,
                    packetDispatchers)
                .Run(env).AsTask();

            await Task.WhenAll(sendingTask, listeningTask);
            return unit;
        });
    }

    private static Aff<RT, Unit> ReadAndProcessMessage<RT>(
        Guid connectionId,
        NetworkStream stream,
        SpotifyEncryptionRecord encryptionRecord,
        Option<ISpotifyListener> listener,
        Ref<HashMap<Guid, Seq<(Guid ListenerId, ChannelWriter<SpotifyPacket> Listener)>>> packetDispatchers)
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
                    return unit;
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
                                connections.Value.TryWrite(pong);
                            });
                        break;
                    case SpotifyPacketType.CountryCode:
                        //handle ourselves
                        var countryCode = Encoding.UTF8.GetString(msg.Packet.Data.Span);
                        listener.Map(x => x.CountryCodeReceived(countryCode));
                        break;
                    case SpotifyPacketType.ProductInfo:
                        //handle ourselves

                        break;
                    case SpotifyPacketType.MercuryEvent or SpotifyPacketType.MercuryReq
                        or SpotifyPacketType.MercurySub or SpotifyPacketType.MercuryUnsub
                        or SpotifyPacketType.AesKey or SpotifyPacketType.AesKeyError:
                        packetDispatchers.Value
                            .Find(x => x.Key == connectionId)
                            .IfSome(dispatchers =>
                            {
                                dispatchers.Value.Iter(dispatcher => { dispatcher.Listener.TryWrite(msg.Packet); });
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

public interface ISpotifyListener
{
    Unit OnConnected(Guid connectionId);
    Unit OnDisconnected(Option<Error> error);
    Unit CountryCodeReceived(string countryCode);
}