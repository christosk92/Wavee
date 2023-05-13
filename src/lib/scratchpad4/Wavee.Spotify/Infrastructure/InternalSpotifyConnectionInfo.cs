using Eum.Spotify;
using Wavee.Spotify.Configs;

namespace Wavee.Spotify.Infrastructure;

internal class InternalSpotifyConnectionInfo
{
    internal readonly Ref<Option<APWelcome>> WelcomeMessage = Ref(Option<APWelcome>.None);
    public required Guid ConnectionId { get; init; }
    public required string Deviceid { get; init; }
    public required SpotifyConfig Config { get; init; }
    public Option<APWelcome> Welcome => WelcomeMessage.Value;
    public IObservable<Option<APWelcome>> WelcomeChanged => WelcomeMessage.OnChange();

    internal InternalSpotifyConnectionInfo With(Option<APWelcome> w)
    {
        atomic(() => WelcomeMessage.Swap(_ => w));
        return this;
    }
}