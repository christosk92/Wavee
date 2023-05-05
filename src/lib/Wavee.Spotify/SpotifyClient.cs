using Eum.Spotify;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Clients.AudioKeys;
using Wavee.Spotify.Clients.Mercury;
using Wavee.Spotify.Clients.SpApi;

namespace Wavee.Spotify;

internal sealed class SpotifyClient<RT> : ISpotifyClient where RT : struct, HasHttp<RT>
{
    private readonly RT _runtime;
    private readonly Guid _connectionId;
    private readonly Ref<Option<uint>> _nextAudioKeySequence = Ref(Option<uint>.None);
    private readonly Ref<Option<ulong>> _nextMercurySequence = Ref(Option<ulong>.None);
    private readonly Ref<Option<APWelcome>> _apWelcome = Ref(Option<APWelcome>.None);

    public SpotifyClient(Guid connectionId, RT runtime)
    {
        _connectionId = connectionId;
        _runtime = runtime;
    }

    public IMercuryClient Mercury => new MercuryClientImpl(_connectionId, _nextMercurySequence);

    public ISpApi InternalApi =>
        _apWelcome.Value.Match(
            Some: welcome => new SpApiClientImpl<RT>(Mercury, _runtime, welcome.CanonicalUsername),
            None: () => throw new Exception("APWelcome not set")
        );
    
    public IAudioKeys AudioKeys => new AudioKeysClientImpl(_connectionId, _nextAudioKeySequence);

    internal ISpotifyClient OnApWelcome(APWelcome apWelcome)
    {
        atomic(() => _apWelcome.Swap(f => apWelcome));
        return this;
    }
}