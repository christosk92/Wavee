using Eum.Spotify;
using Wavee.Spotify.Clients.AudioKeys;
using Wavee.Spotify.Clients.Mercury;
using Wavee.Spotify.Clients.SpApi;

namespace Wavee.Spotify;

public interface ISpotifyClient
{
    IMercuryClient Mercury { get; }
    ISpApi InternalApi { get; }
    IAudioKeys AudioKeys { get; }

    Option<string> CountryCode { get; }

    Option<APWelcome> WelcomeMessage { get; }


    IObservable<Option<string>> CountryCodeChanged { get; }
    IObservable<Option<APWelcome>> WelcomeMessageChanged { get; }
    Guid ConnectionId { get; }
}
