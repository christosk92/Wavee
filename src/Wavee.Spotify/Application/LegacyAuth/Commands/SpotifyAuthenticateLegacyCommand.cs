using Eum.Spotify;
using Mediator;

namespace Wavee.Spotify.Application.LegacyAuth.Commands;

/// <summary>
/// Authenticate with Spotify using the TCP protocol.
/// This will not keep the socket open, but will instead close it after the authentication is complete.
/// 
/// This is needed to fetch a strong authentication token.
/// 
/// <exception>
///     <cref>SpotifyLegacyAuthenticationException</cref>
/// </exception>
/// </summary>
public sealed class SpotifyAuthenticateLegacyCommand : ICommand<APWelcome>
{
    public required string DeviceId { get; init; }
    public required LoginCredentials Credentials { get; init; }
}