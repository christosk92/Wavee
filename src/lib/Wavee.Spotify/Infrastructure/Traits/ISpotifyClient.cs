using Eum.Spotify;
using Wavee.Spotify.Infrastructure.Common;
using Wavee.Spotify.Infrastructure.Common.Mercury;
using Wavee.Spotify.Infrastructure.Common.Token;
using Wavee.Spotify.Models;

namespace Wavee.Spotify.Infrastructure.Traits;

/// <summary>
/// A client for everything Spotify.
/// </summary>
public interface ISpotifyClient
{
    /// <summary>
    /// Interface with the Mercury protocol (hm://...).
    /// </summary>
    IMercuryClient Mercury { get; }

    /// <summary>
    /// Set of methods to get a token. Includes a cache.
    /// </summary>
    ITokenProvider Token { get; }

    /// <summary>
    /// Connect to Spotify using the given credentials.
    /// </summary>
    /// <param name="credentials">The credentials to sign in with.</param>
    /// <param name="cancellationToken">Not used yet</param>
    /// <returns>A welcome message</returns>
    ValueTask<APWelcome> Connect(LoginCredentials credentials, CancellationToken cancellationToken = default);

    /// <summary>
    /// The current connection state.
    /// </summary>
    ConnectionState ConnectionState { get; }

    /// <summary>
    /// Maybe the country code of the current user.
    /// </summary>
    Option<string> CountryCode { get; }

    /// <summary>
    /// Maybe the product info of the current user.
    /// </summary>
    Option<HashMap<string, string>> ProductInfo { get; }

    /// <summary>
    /// An observable that emits the current connection state.
    /// </summary>
    IObservable<ConnectionState> ConnectionStateChanged { get; }

    /// <summary>
    /// An observable that emits the current country code.
    /// </summary>
    IObservable<Option<string>> CountryCodeChanged { get; }

    /// <summary>
    /// An observable that emits the current product info.
    /// </summary>
    IObservable<Option<HashMap<string, string>>> ProductInfoChanged { get; }

    SpotifySessionConfig Config { get; }
}