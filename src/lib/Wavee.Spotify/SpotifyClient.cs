using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Xml;
using Eum.Spotify;
using LanguageExt;
using Wavee.Spotify.Infrastructure.ApResolver;
using Wavee.Spotify.Infrastructure.Authentication;
using Wavee.Spotify.Infrastructure.Cache;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Spotify.Infrastructure.Connection.Crypto;
using Wavee.Spotify.Infrastructure.Mercury;
using Wavee.Spotify.Infrastructure.Mercury.Token;
using Wavee.Spotify.Infrastructure.Playback;
using Wavee.Spotify.Infrastructure.Playback.Key;
using Wavee.Spotify.Infrastructure.Remote;
using Wavee.Spotify.Infrastructure.Tcp;

namespace Wavee.Spotify;

public sealed class SpotifyClient
{
    private static readonly
        AtomHashMap<Guid, Seq<(Func<SpotifySendPacket, bool> DispatchConditional, Channel<SpotifySendPacket>
            Dispatcher)>> Subscribers =
            AtomHashMap<Guid, Seq<(Func<SpotifySendPacket, bool> DispatchConditional, Channel<SpotifySendPacket>
                Dispatcher)>>();

    private readonly ChannelWriter<SpotifySendPacket> _sender;
    private readonly Guid _connectionId;
    private readonly SpotifyConfig _config;
    private readonly string _deviceId;
    private readonly Ref<Option<string>> _country = Ref(Option<string>.None);
    private readonly Ref<HashMap<string, string>> _productInfo = Ref(LanguageExt.HashMap<string, string>.Empty);

    private SpotifyClient(
        NetworkStream networkStream,
        SpotifyAuthenticationResult authenticationResult,
        SpotifyConfig config, string deviceId)
    {
        _config = config;
        _deviceId = deviceId;
        _connectionId = Guid.NewGuid();
        WelcomeMessage = authenticationResult.WelcomeMessage;
        AddPingPongListener();
        AddWelcomeStuffListener();

        var producerToTcp = Channel.CreateUnbounded<SpotifySendPacket>();
        _sender = producerToTcp.Writer;

        StartProducer(networkStream, producerToTcp.Reader, authenticationResult.ConnectionRecord);
        StartConsumer(_connectionId, networkStream, authenticationResult.ConnectionRecord);

        RemoteClient = new SpotifyRemoteClient(
            tokenClient: TokenClient,
            mercuryClient: MercuryClient,
            config: _config.Remote,
            deviceId: _deviceId
        );

        PlaybackClient = new SpotifyPlaybackClient(
            tokenClient: TokenClient,
            mercuryClient: MercuryClient,
            audioKeyClient: AudioKeyClient,
            cacheFactory: () => Cache,
            config: _config.Playback,
            country: _country,
            productInfo: _productInfo
        );
    }

    public APWelcome WelcomeMessage { get; }

    public MercuryClient MercuryClient =>
        new MercuryClient(
            connectionId: _connectionId,
            subscribe: OnPackageSubscriptionRequested,
            removePackageListener: OnpackageRemoveSubscriptionRequested
        );

    public AudioKeyClient AudioKeyClient =>
        new AudioKeyClient(
            connectionId: _connectionId,
            subscribe: OnPackageSubscriptionRequested,
            removePackageListener: OnpackageRemoveSubscriptionRequested
        );

    public TokenClient TokenClient =>
        new TokenClient(
            connectionId: _connectionId,
            mercuryClient: MercuryClient
        );
    public SpotifyCache Cache =>
        new SpotifyCache(_config.CachePath);
    public SpotifyRemoteClient RemoteClient { get; }
    public SpotifyPlaybackClient PlaybackClient { get; }
    public Option<string> CountryCode => _country.Value;
    public HashMap<string, string> ProductInfo => _productInfo.Value;

    private void OnpackageRemoveSubscriptionRequested(ChannelReader<SpotifySendPacket> reader)
    {
        //remove from subscribers and complete writer
        //var writer = _subscribers.Find(x => x.Item2 == reader).IfNone(() => throw new Exception("not found")).Item2;
        var writer = Subscribers.Find(_connectionId)
            .IfNone(() => throw new Exception("not found"))
            .Find(x => x.Dispatcher.Reader == reader)
            .IfNone(() => throw new Exception("not found"))
            .Dispatcher.Writer;

        writer.Complete();

        Subscribers.AddOrUpdate(
            _connectionId,
            Some: s => s.Filter(x => x.Dispatcher.Reader != reader),
            None: () => throw new Exception("not found")
        );
    }

    private ChannelReader<SpotifySendPacket> OnPackageSubscriptionRequested(
        Option<SpotifySendPacket> write,
        Func<SpotifySendPacket, bool> arg)
    {
        var channel = Channel.CreateUnbounded<SpotifySendPacket>();
        var reader = channel.Reader;
        //_subscribers.Add((arg, channel.Writer));
        Subscribers.AddOrUpdate(
            _connectionId,
            Some: s => s.Add((arg, channel)),
            None: () => Seq1((arg, channel))
        );

        write.IfSome(x => _sender.TryWrite(x));
        return reader;
    }

    private void AddWelcomeStuffListener()
    {
        var listener = OnPackageSubscriptionRequested(
            None,
            p => p.Command is SpotifyPacketType.CountryCode or SpotifyPacketType.ProductInfo
        );
        Task.Factory.StartNew(async () =>
        {
            await foreach (var packet in listener.ReadAllAsync())
            {
                switch (packet.Command)
                {
                    case SpotifyPacketType.CountryCode:
                        var countryCode = Encoding.UTF8.GetString(packet.Data.Span);
                        Console.WriteLine($"CountryCode: {countryCode}");
                        atomic(() => _country.Swap(_ => countryCode));
                        break;
                    case SpotifyPacketType.ProductInfo:
                        Console.WriteLine("ProductInfo");
                        var productInfo = Encoding.UTF8.GetString(packet.Data.Span);
                        var xml = new XmlDocument();
                        xml.LoadXml(productInfo);

                        var products = xml.SelectNodes("products");
                        var dc = new HashMap<string, string>();
                        if (products != null && products.Count > 0)
                        {
                            var firstItemAsProducts = products[0];

                            var product = firstItemAsProducts.ChildNodes[0];

                            var properties = product.ChildNodes;
                            for (var i = 0; i < properties.Count; i++)
                            {
                                var node = properties.Item(i);
                                dc = dc.Add(node.Name, node.InnerText);
                            }
                        }

                        atomic(() => _productInfo.Swap(_ => dc));
                        break;
                }
            }
        }, TaskCreationOptions.LongRunning);
    }

    private void AddPingPongListener()
    {
        var listener = OnPackageSubscriptionRequested(
            None,
            p => p.Command is SpotifyPacketType.Ping or SpotifyPacketType.PongAck
        );

        Task.Factory.StartNew(async () =>
        {
            var empty4bytearray = new byte[4];
            await foreach (var packet in listener.ReadAllAsync())
            {
                switch (packet.Command)
                {
                    case SpotifyPacketType.Ping:
                        _sender.TryWrite(new SpotifySendPacket(SpotifyPacketType.Pong, empty4bytearray));
                        break;
                    case SpotifyPacketType.PongAck:
                        Console.WriteLine("PongAck");
                        break;
                }
            }
        }, TaskCreationOptions.LongRunning);
    }


    private static void StartProducer(NetworkStream stream,
        ChannelReader<SpotifySendPacket> sender,
        SpotifyConnectionRecord record)
    {
        Task.Factory.StartNew(async () =>
        {
            while (true)
            {
                try
                {
                    await foreach (var packet in sender.ReadAllAsync())
                    {
                        record = SpotifyTcp.Send(
                            stream,
                            record,
                            packet.Command,
                            packet.Data.Span
                        );
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    await Task.Delay(4000);
                }
            }
        }, TaskCreationOptions.LongRunning);
    }

    private static void StartConsumer(
        Guid connectionId,
        NetworkStream stream,
        SpotifyConnectionRecord record)
    {
        //consumption (tcp -> consumer)
        Task.Factory.StartNew(() =>
        {
            while (true)
            {
                try
                {
                    var received = SpotifyTcp.Receive(stream, ref record);
                    var toSendPacket = new SpotifySendPacket(
                        (SpotifyPacketType)received.Header[0],
                        received.Payload.ToArray());
                    bool dispatched = false;
                    foreach (var (dispatchConditional, dispatcher) in Subscribers[connectionId])
                    {
                        if (dispatchConditional(toSendPacket))
                        {
                            dispatcher.Writer.TryWrite(toSendPacket);
                            dispatched = true;
                        }
                    }

                    if (!dispatched)
                    {
                        Console.WriteLine($"No dispatcher for {toSendPacket.Command}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    //TODO: Attempt to reconnect
                }
            }
        }, TaskCreationOptions.LongRunning);
    }


    public static async Task<SpotifyClient> CreateAsync(LoginCredentials credentials,
        SpotifyConfig config,
        CancellationToken ct = default)
    {
        var (host, port) = await ApResolve.GetAccessPoint(ct);
        var deviceId = Guid.NewGuid().ToString();
        var tcp = SpotifyConnection.Connect(host, port);
        var stream = tcp.GetStream();
        var handshakeResult = SpotifyConnection.Handshake(stream);
        var authenticationResult = Authenticate.PerformAuth(stream, credentials, handshakeResult, deviceId);

        return new SpotifyClient(
            stream,
            authenticationResult,
            config,
            deviceId);
    }
}