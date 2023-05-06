using System.Net.WebSockets;
using System.Text;
using Eum.Spotify;
using Eum.Spotify.connectstate;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Clients.AudioKeys;
using Wavee.Spotify.Clients.Mercury;
using Wavee.Spotify.Clients.SpApi;
using Wavee.Spotify.Infrastructure.ApResolver;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Spotify.Infrastructure.Sys;

namespace Wavee.Spotify;

internal sealed class SpotifyClient<RT> : ISpotifyClient where RT : struct, HasHttp<RT>, HasWebsocket<RT>
{
    private readonly CancellationTokenSource _cts = new();
    private readonly RT _runtime;
    private readonly Guid _connectionId;
    private readonly Ref<Option<uint>> _nextAudioKeySequence = Ref(Option<uint>.None);
    private readonly Ref<Option<ulong>> _nextMercurySequence = Ref(Option<ulong>.None);
    private readonly Ref<Option<APWelcome>> _apWelcome = Ref(Option<APWelcome>.None);
    private readonly Ref<Option<string>> _countryCodeRef = Ref(Option<string>.None);

    public SpotifyClient(Guid connectionId, RT runtime)
    {
        _connectionId = connectionId;
        _runtime = runtime;

        //setup a listener for country code
        var listenerResult = SpotifyRuntime.GetChannelReader(_connectionId);
        Task.Run(async () =>
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    await foreach (var package in listenerResult.ReadAllAsync(_cts.Token))
                    {
                        _ = package
                            .Match(Left: _ => unit,
                                Right: packet =>
                                {
                                    if (packet.Command is not SpotifyPacketType.CountryCode)
                                        return unit;

                                    var countryCode = Encoding.UTF8.GetString(packet.Data.Span);

                                    atomic(() => _countryCodeRef.Swap(f => countryCode));
                                    return unit;
                                });
                    }
                }
            }
            finally
            {
                SpotifyRuntime.RemoveListener(_connectionId, listenerResult);
            }
        });
    }

    public IMercuryClient Mercury => new MercuryClientImpl<RT>(_connectionId,
        _nextMercurySequence,
        _countryCodeRef,
        _apWelcome.Value.Map(x => x.CanonicalUsername),
        _runtime);

    public ISpApi InternalApi =>
        _apWelcome.Value.Match(
            Some: welcome => new SpApiClientImpl<RT>(Mercury, _runtime, welcome.CanonicalUsername),
            None: () => throw new Exception("APWelcome not set")
        );

    public IAudioKeys AudioKeys => new AudioKeysClientImpl(_connectionId, _nextAudioKeySequence);
    public Option<string> CountryCode => _countryCodeRef.Value;
    public Option<APWelcome> WelcomeMessage => _apWelcome.Value;
    public IObservable<Option<string>> CountryCodeChanged => _countryCodeRef.OnChange();
    public IObservable<Option<APWelcome>> WelcomeMessageChanged => _apWelcome.OnChange();
    public Guid ConnectionId => _connectionId;

    internal ISpotifyClient OnApWelcome(APWelcome apWelcome)
    {
        atomic(() => _apWelcome.Swap(f => apWelcome));

        //connect to websocket
        Task.Run(() => ConnectToWs(() => this.Mercury, () => _runtime));
        return this;
    }

    private static async Task ConnectToWs(
        Func<IMercuryClient> mercuryFactory,
        Func<RT> runtimeFactory)
    {
        var affResult = await ConnectToWsAndUpdateState<RT>(mercuryFactory).Run(runtimeFactory());
        affResult.Match(
            Succ: valueTuple =>
            {

            },
            Fail: error =>
            {
                throw error;
            });
    }

    private static Aff<RT, (Cluster, WebSocket, string)> ConnectToWsAndUpdateState<RT>(Func<IMercuryClient> mercuryClient)
        where RT : struct, HasHttp<RT>, HasWebsocket<RT> =>
        from token in cancelToken<RT>()
        from bearer in mercuryClient().FetchBearer(token).ToAff()
        from wssUrl in AP<RT>.FetchDealer()
            .Map(f => $"wss://{f.Host}:{f.Port}?access_token={bearer}")
        from websocketClient in Ws<RT>.Connect(wssUrl)
        select (new Cluster(), websocketClient, bearer);
    //         let stream = tcpClient.GetStream()
    //         from clientHelloResult in Handshake<RT>.PerformClientHello(stream)
    //         from nonceAfterAuthAndApWelcome in Authentication<RT>.Authenticate(stream, clientHelloResult, credentials,
    //             deviceId)
    //         select (nonceAfterAuthAndApWelcome.ApWelcome, stream, nonceAfterAuthAndApWelcome.EncryptionRecord);
}