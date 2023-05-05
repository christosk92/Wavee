using System.Text;
using Eum.Spotify;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Clients.AudioKeys;
using Wavee.Spotify.Clients.Mercury;
using Wavee.Spotify.Clients.SpApi;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Spotify.Infrastructure.Sys;

namespace Wavee.Spotify;

internal sealed class SpotifyClient<RT> : ISpotifyClient where RT : struct, HasHttp<RT>
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

    public IMercuryClient Mercury => new MercuryClientImpl(_connectionId, _nextMercurySequence, _countryCodeRef);

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
        return this;
    }
}