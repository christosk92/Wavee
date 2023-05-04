using Eum.Spotify;
using Wavee.Spotify.Clients.Mercury;

namespace Wavee.Spotify;

internal sealed class SpotifyClient<RT> : ISpotifyClient where RT : struct
{
    private readonly RT _runtime;
    private readonly Guid _connectionId;
    private readonly Ref<Option<ulong>> _nextMercurySequence = Ref(Option<ulong>.None);
    private readonly Ref<Option<APWelcome>> _apWelcome = Ref(Option<APWelcome>.None);

    public SpotifyClient(Guid connectionId, RT runtime)
    {
        _connectionId = connectionId;
        _runtime = runtime;
    }

    public IMercuryClient Mercury => new MercuryClientImpl(_connectionId, _nextMercurySequence);

    internal ISpotifyClient OnApWelcome(APWelcome apWelcome)
    {
        atomic(() => _apWelcome.Swap(f => apWelcome));
        return this;
    }
}