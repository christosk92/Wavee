using Eum.Spotify.login5v3;
using Mediator;

namespace Wavee.Spotify.Application.Authentication.Requests;

public sealed class SpotifyLoginV3Request : IRequest<LoginResponse>
{
    public required StoredCredential Request { get; init; }
    public required string DeviceId { get; init; }
}