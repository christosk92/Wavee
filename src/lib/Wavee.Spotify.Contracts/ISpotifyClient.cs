using Eum.Spotify;
using LanguageExt;
using Wavee.Spotify.Contracts.AudioKeys;
using Wavee.Spotify.Contracts.Mercury;
using Wavee.Spotify.Contracts.Remote;
using Wavee.Spotify.Contracts.SpApi;

namespace Wavee.Spotify.Contracts;

public interface ISpotifyClient
{
    IMercuryClient Mercury { get; }
    ISpApi InternalApi { get; }
    IAudioKeys AudioKeys { get; }
    Option<ISpotifyRemoteClient> RemoteClient { get; }
    Option<string> CountryCode { get; }
    Option<APWelcome> WelcomeMessage { get; }
    IObservable<Option<ISpotifyRemoteClient>> RemoteClientChanged { get; }
    IObservable<Option<string>> CountryCodeChanged { get; }
    IObservable<Option<APWelcome>> WelcomeMessageChanged { get; }
    Guid ConnectionId { get; }
}