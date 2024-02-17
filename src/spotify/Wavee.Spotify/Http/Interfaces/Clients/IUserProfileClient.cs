using Wavee.Spotify.Exceptions;
using Wavee.Spotify.Models.Response;

namespace Wavee.Spotify.Http.Interfaces.Clients;

/// <summary>
///   Endpoints for retrieving information about a user’s profile.
/// </summary>
/// <remarks>https://developer.spotify.com/documentation/web-api/reference-beta/#category-user-profile</remarks>
public interface IUserProfileClient
{
    /// <summary>
    ///   Get detailed profile information about the current user (including the current user’s username).
    /// </summary>
    /// <param name="cancel">The cancellation-token to allow to cancel the request.</param>
    /// <remarks>
    /// https://developer.spotify.com/documentation/web-api/reference-beta/#endpoint-get-current-users-profile
    /// </remarks>
    /// <exception cref="ApiUnauthorizedException">Thrown if the client is not authenticated.</exception>
    Task<Me> Current(CancellationToken cancel = default);
}