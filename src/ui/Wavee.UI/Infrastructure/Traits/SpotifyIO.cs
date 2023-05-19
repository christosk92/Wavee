using Eum.Spotify;
using LanguageExt;
using LanguageExt.Attributes;
using LanguageExt.Effects.Traits;
using Wavee.Spotify.Infrastructure.Cache;
using Wavee.Spotify.Infrastructure.Mercury;
using Wavee.Spotify.Infrastructure.Remote;
using Wavee.Spotify.Infrastructure.Remote.Messaging;

namespace Wavee.UI.Infrastructure.Traits;

public interface SpotifyIO
{
    ValueTask<Unit> Authenticate(LoginCredentials credentials, CancellationToken ct = default);
    Option<APWelcome> WelcomeMessage();
    Option<IObservable<SpotifyRemoteState>> ObserveRemoteState();
    Option<SpotifyCache> Cache();
    Option<string> CountryCode();
    Option<string> CdnUrl();
    MercuryClient Mercury();
    Option<string> GetOwnDeviceId();
    Option<SpotifyRemoteClient> GetRemoteClient();
}

/// <summary>
/// Type-class giving a struct the trait of supporting spotify IO
/// </summary>
/// <typeparam name="RT">Runtime</typeparam>
[Typeclass("*")]
public interface HasSpotify<RT> : HasCancel<RT>
    where RT : struct, HasCancel<RT>
{
    /// <summary>
    /// Access the spotify synchronous effect environment
    /// </summary>
    /// <returns>Spotify synchronous effect environment</returns>
    Eff<RT, SpotifyIO> SpotifyEff { get; }
}
