using Mediator;
using Wavee.Spotify.Domain.Exceptions;

namespace Wavee.Spotify.Application.Authentication.Queries;

/// <summary>
/// A query to get a Spotify token.
/// <exception cref="SpotifyNotAuthenticatedException"/> 
/// </summary>
public sealed class GetSpotifyTokenQuery : IRequest<string>
{
    public string? Username { get; init; }
}