using Eum.Spotify;
using Wavee.Spotify.Clients.AudioKeys;
using Wavee.Spotify.Clients.Mercury;
using Wavee.Spotify.Clients.SpApi;
using Wavee.Spotify.Remote;
using Wavee.Spotify.Remote.Infrastructure.Sys;

namespace Wavee.Spotify;

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