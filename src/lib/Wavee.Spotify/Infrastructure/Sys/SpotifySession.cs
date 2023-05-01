using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Xml;
using Eum.Spotify;
using LanguageExt.UnsafeValueAccess;
using Wavee.Spotify.Infrastructure.Common;
using Wavee.Spotify.Infrastructure.Common.Mercury;
using Wavee.Spotify.Infrastructure.Common.Token;
using Wavee.Spotify.Infrastructure.Traits;
using Wavee.Spotify.Models;
using Wavee.Spotify.Models.Internal;
using Seq = LanguageExt.Seq;

namespace Wavee.Spotify.Infrastructure.Sys;

// ReSharper disable once InconsistentNaming
internal sealed class SpotifySession<RT> where RT : struct, HasTCP<RT>
{
    private readonly RT _runtime;

    // |-----------------------------------------|
    // |         Generic connection              |
    // |-----------------------------------------|
    private readonly ManualResetEvent _connectionEstablished = new(false);
    public readonly Ref<Option<string>> CountryCode = Ref(Option<string>.None);
    public readonly Ref<Option<HashMap<string, string>>> ProductInfo = Ref(Option<HashMap<string, string>>.None);
    public readonly Ref<ConnectionState> ConnectionState = Ref(Common.ConnectionState.NotConnected);
    private readonly CancellationTokenSource _cts = new();


    // |-----------------------------------------|
    // |            Tokens                       |
    // |-----------------------------------------|
    private readonly Atom<Seq<BearerToken>> _tokens = Atom(Seq.empty<BearerToken>());


    // |-----------------------------------------|
    // |               TCP                       |
    // |-----------------------------------------|
    private readonly Atom<Option<uint>> _sendNonce = Atom(Option<uint>.None);
    private readonly Atom<Option<uint>> _recvNonce = Atom(Option<uint>.None);
    private readonly Atom<Option<ReadOnlyMemory<byte>>> _receiveKey = Atom(Option<ReadOnlyMemory<byte>>.None);
    private readonly Atom<Option<ReadOnlyMemory<byte>>> _sendKey = Atom(Option<ReadOnlyMemory<byte>>.None);
    private readonly BlockingCollection<SpotifyPacket> _sendQueue; //TODO: Replace this with a functional queue

    // |-----------------------------------------|
    // |               Mercury                   |
    // |-----------------------------------------|
    private readonly Ref<HashMap<ulong, MercuryPending>> _mercuryCallbacks =
        Ref(LanguageExt.HashMap<ulong, MercuryPending>.Empty);

    private readonly Atom<Option<ulong>> _mercurySeq = Atom(Option<ulong>.None);

    public SpotifySession(RT runtime)
    {
        _runtime = runtime;
        _sendQueue = new BlockingCollection<SpotifyPacket>();

        //we need to setup a listener for the TCP connection
        //basically we continously read from the TCP connection (if any)

        //so we need a way to signal a thread that a connection has been established
        Task.Factory.StartNew(async () =>
        {
            while (!_cts.IsCancellationRequested)
            {
                await Task.Run(() => _connectionEstablished.WaitOne(), _cts.Token);
                //we have a connection, so we need to read from it
                var aff =
                    from receiveKey in _receiveKey.Value.ToEff("Connection not established")
                    let recvNonce = _recvNonce.Value
                    from k in SpotifyConnection<RT>
                        .ReceiveDecryptedPackage(receiveKey, recvNonce)
                        .Map(p =>
                        {
                            var (packet, nextRecvNonce) = p;
                            _recvNonce.Swap(_ => nextRecvNonce);
                            // swap(_recvNonce, uints => nextRecvNonce);

                            return atomic(() => DistributePacket(packet));
                        })
                    from _ in k
                    select unit;

                var result = await aff.Run(_runtime);
                _ = HandleConnectionEfResult(result);
            }
        });

        //same for the send queue
        Task.Factory.StartNew(async () =>
        {
            while (!_cts.IsCancellationRequested)
            {
                await Task.Run(() => _connectionEstablished.WaitOne(), _cts.Token);
                //we have a connection, so we need to read from it
                var aff =
                    from sendKey in _sendKey.Value.ToEff("Connection not established")
                    let sendNonce = _sendNonce.Value
                    from packet in _sendQueue.TryTake(out var packet, -1, _cts.Token)
                        ? SuccessEff(packet)
                        : FailEff<SpotifyPacket>("No packet to send")
                    from _ in SpotifyConnection<RT>
                        .SendEncryptedPackage(packet, sendKey, sendNonce)
                        .Map(nextSendNonce => _sendNonce.Swap(_ => nextSendNonce))
                    select unit;

                var result = await aff.Run(_runtime);
                _ = HandleConnectionEfResult(result);
            }
        });
    }


    //Core
    public async ValueTask<APWelcome> Connect(string deviceId, LoginCredentials credentials,
        CancellationToken cancellationToken)
    {
        //ap-gae2.spotify.com:4070
        const string host = "ap-gae2.spotify.com";
        const ushort port = 4070;
        var task =
            from keys in SpotifyConnection<RT>
                .ConnectButNoAuthenticate(host, port)
            from apWelcomeAndNonces in SpotifyConnection<RT>
                .Authenticate(deviceId, credentials, keys.SendKey, keys.ReceiveKey)
            select (apWelcomeAndNonces, keys);

        var result = await task.Run(_runtime);

        var apWelcome = result.Match(
            Succ: msg =>
            {
                //replace the nonces

                _sendNonce.Swap(_ => msg.apWelcomeAndNonces.SendNonce);
                _recvNonce.Swap(_ => msg.apWelcomeAndNonces.Receivenonce);

                _sendKey.Swap(_ => msg.keys.SendKey);
                _receiveKey.Swap(_ => msg.keys.ReceiveKey);

                return msg.apWelcomeAndNonces.Welcome;
            },
            Fail: error =>
            {
                atomic(() => ConnectionState.Swap(_ => Common.ConnectionState.NotConnected));
                _connectionEstablished.Reset();
                throw new Exception(error.Message);
            }
        );

        atomic(() => ConnectionState.Swap(_ => Common.ConnectionState.Connected));
        _connectionEstablished.Set();
        return apWelcome;
    }

    #region Connection

    //TODO: Convert from impure functions to actual pure functions
    private Eff<Unit> DistributePacket(SpotifyPacket packet) =>
        Eff(() => packet.Command switch
        {
            PacketType.Ping => PingHandler(packet),
            PacketType.PongAck => PongAckHandler(),
            PacketType.CountryCode => CountryCodeHandler(packet),
            PacketType.ProductInfo => ProductInfoHandler(packet),
            PacketType.MercuryEvent or PacketType.MercuryReq or PacketType.MercurySub or PacketType.MercuryUnsub =>
                MercuryClient<RT>.Handle(packet, _mercuryCallbacks),
            _ => UnknownPacketHandler(packet)
        });

    private Unit UnknownPacketHandler(SpotifyPacket packet)
    {
        Debug.WriteLine($"Unknown packet received: {packet.Command}");
        return unit;
    }

    private Unit PingHandler(SpotifyPacket packet)
    {
        //enqueue a pong
        var timestamp = BinaryPrimitives.ReadUInt32BigEndian(packet.Payload.Span);
        Debug.WriteLine($"Ping received: {timestamp}");
        var newPacket = new SpotifyPacket(PacketType.Pong, new byte[4]);
        _sendQueue.Add(newPacket);
        return unit;
    }

    private Unit PongAckHandler()
    {
        Debug.WriteLine($"PongAck received");
        return unit;
    }

    private Unit CountryCodeHandler(SpotifyPacket packet)
    {
        var countryCode = Encoding.UTF8.GetString(packet.Payload.Span);
        Debug.WriteLine($"CountryCode received: {countryCode}");
        atomic(() => CountryCode.Swap(_ => countryCode));
        return unit;
    }

    private Unit ProductInfoHandler(SpotifyPacket packet)
    {
        var productInfoStr = Encoding.UTF8.GetString(packet.Payload.Span);
        //xml parse

        var dict = new HashMap<string, string>();
        var xml = new XmlDocument();
        xml.LoadXml(productInfoStr);

        var products = xml.SelectNodes("products");

        if (products is { Count: > 0 })
        {
            var firstItemAsProducts = products[0];

            var product = firstItemAsProducts!.ChildNodes[0];

            var properties = product!.ChildNodes;
            for (var i = 0; i < properties.Count; i++)
            {
                var node = properties.Item(i);
                dict = dict.Add(node!.Name, node.InnerText);
                //attributes[node.Name] = node.InnerText;
            }
        }

        atomic(() => ProductInfo.Swap(_ => dict));
        return unit;
    }


    private Unit HandleConnectionEfResult(Fin<Unit> result) =>
        result.Match(
            Succ: _ => unit,
            Fail: _ =>
            {
                //TODO: Check for specific error
                //connection died
                Debugger.Break();
                _connectionEstablished.Reset();

                _sendNonce.Swap(_ => Option<uint>.None);
                _recvNonce.Swap(_ => Option<uint>.None);

                _sendKey.Swap(_ => Option<ReadOnlyMemory<byte>>.None);
                _receiveKey.Swap(_ => Option<ReadOnlyMemory<byte>>.None);
                atomic(() => ConnectionState.Swap(_ => Common.ConnectionState.NotConnected));
                return unit;
            });

    #endregion

    #region Mercury

    public IMercuryClient Mercury =>
        new MercuryClient<RT>(SendFromMercury, RegisterMercuryCallback, GetMercurySeq, _runtime);

    public ITokenProvider TokenProvider =>
        new TokenProvider<RT>((MercuryClient<RT>)Mercury, _runtime, HasTokenInCache, Cache);
    

    private Unit Cache(MercuryTokenData arg)
    {
        _tokens.Swap(x => x.Add(new BearerToken(
            arg.AccessToken,
            TimeSpan.FromSeconds(arg.ExpiresIn),
            BearerToken.MERCURY_TOKEN_TYPE,
            arg.Scope,
            DateTimeOffset.UtcNow
        )));
        return unit;
    }

    private Option<string> HasTokenInCache()
        => _tokens.Value.Find(c => c is { TokenType: BearerToken.MERCURY_TOKEN_TYPE, Expired: false })
            .Map(s => s.AccessToken);

    private Eff<Option<ulong>> GetMercurySeq()
    {
        return atomic(Eff(() =>
        {
            //if none: return none, and then set zero
            //if 0, return 1 and set 1
            //if 1, return 2 and set 2
            //if 2, return 3 and set 3

            var seq = _mercurySeq.Value;
            if (seq.IsNone)
            {
                _mercurySeq.Swap(_ => Option<ulong>.Some(0));
                return Option<ulong>.None;
            }

            var seqVal = seq.ValueUnsafe();
            _mercurySeq.Swap(_ => Option<ulong>.Some(seqVal)).ValueUnsafe();
            return Option<ulong>.Some(seqVal + 1);
        }));
    }

    private Eff<Task<MercuryResponse>> RegisterMercuryCallback(ulong arg)
    {
        //_mercuryCallbacks
        var tcs = new TaskCompletionSource<MercuryResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        return atomic(Eff(() =>
        {
            var pending = new MercuryPending(
                Parts: LanguageExt.Seq<ReadOnlyMemory<byte>>.Empty,
                Partial: None,
                Callback: tcs,
                Flag: false
            );
            _mercuryCallbacks.Swap(x => x.AddOrUpdate(arg, pending));
            return tcs.Task;
        }));
    }

    private Aff<RT, Unit> SendFromMercury(SpotifyPacket arg) =>
        atomic(Eff(() =>
        {
            _sendQueue.Add(arg);
            return unit;
        }));

    #endregion
}