using Wavee.Models.Token;

namespace Wavee.Interfaces;

/// <summary>
/// Defines an interface for a client responsible for retrieving Spotify tokens.
/// </summary>
public interface ISpotifyTokenClient
{
    /// <summary>
    /// Retrieves a bearer token from the Spotify authorization service.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation that returns the SpotifyToken upon completion.
    /// </returns>
    ValueTask<SpotifyToken> GetBearerToken(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a client token from the Spotify authorization service.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation that returns the SpotifyToken upon completion.
    /// </returns>
    ValueTask<SpotifyToken> GetClientToken(CancellationToken cancellationToken = default);
}