using Wavee.Spotify.Common;

namespace Wavee.Spotify.Artist;

/// <summary>
/// A lightweight wrapper around the Spotify partner API for artist-related endpoints.
/// </summary>
public interface IArtistClient
{
    /// <summary>
    /// Fetches an artist by ID.
    /// The discography is paginated, so you will need to call <see cref="GetDiscographyAsync"/> to get the full discography.
    /// </summary>
    /// <param name="id">
    /// The ID of the artist.
    /// </param>
    /// <param name="cancellationToken">
    /// A token which may be used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// A <see cref="SpotifyArtist"/> object representing the artist.
    /// </returns>
    Task<SpotifyArtist> GetArtistAsync(SpotifyId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the discography of an artist for a given type.
    /// </summary>
    /// <param name="id">The id of the artist.</param>
    /// <param name="type">
    /// The type of discography to fetch (albums, singles, compilations).
    /// </param>
    /// <param name="offset">
    /// The offset to start fetching from.
    /// </param>
    /// <param name="limit">
    /// The maximum number of items to fetch. This is capped at 100.
    /// </param>
    /// <param name="cancellationToken">
    /// A token which may be used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// A <see cref="Paged{T}"/> object containing the discography.
    /// </returns>
    Task<Paged<SpotifyArtistDiscographyReleaseWrapper>> GetDiscographyAsync(SpotifyId id, DiscographyType type, int offset, int limit, CancellationToken cancellationToken = default);
}