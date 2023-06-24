using Spotify.Metadata;
using Wavee.Id;
using Wavee.Infrastructure.Mercury;
using Wavee.Metadata.Artist;
using Wavee.Metadata.Me;

namespace Wavee.Metadata;

/// <summary>
/// A client for the Spotify Metadata API. 
/// </summary>
public interface ISpotifyMetadataClient
{
    /// <summary>
    /// Gets a <see cref="Track"/> by its <see cref="SpotifyId"/>.
    /// </summary>
    /// <param name="id">
    /// The <see cref="SpotifyId"/> of the <see cref="Track"/> to get.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that will complete with the <see cref="Track"/> or throw a <see cref="MercuryException"/>.
    /// Sometimes (Due to track relinking), the <see cref="Track"/> returned will not match the <see cref="SpotifyId"/> requested, but rather will be the relinked track.
    /// </returns>
    Task<Track> GetTrack(SpotifyId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches an <see cref="ArtistOverview"/> for the given <see cref="SpotifyId"/>
    /// As seen in the Spotify Desktop Client, this is a summary of the artist, including:
    /// - Biography
    /// - Top Tracks
    /// - Related Artists
    /// - Albums
    /// - Singles
    /// - Appears On
    /// </summary>
    /// <param name="artistId">
    /// The <see cref="SpotifyId"/> of the <see cref="Artist"/> to get.
    /// </param>
    /// <param name="destroyCache">
    /// A boolean indicating whether or not to destroy the cache for this artist.
    /// </param>
    /// <param name="ct">
    /// A <see cref="CancellationToken"/> to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that will complete with the <see cref="ArtistOverview"/> or throw a <see cref="MercuryException"/>.
    /// </returns>
    ValueTask<ArtistOverview> GetArtistOverview(SpotifyId artistId, bool destroyCache, CancellationToken ct = default);

    /// <summary>
    /// Performs an authenticated query to fetch information about the current user.
    /// </summary>
    /// <param name="ct">
    /// A <see cref="CancellationToken"/> to cancel the operation.
    /// </param>
    /// <returns>
    /// If successful, a <see cref="Task{TResult}"/> that will complete with the <see cref="MeUser"/> or throw a <see cref="MercuryException"/>.
    /// </returns>
    Task<MeUser> GetMe(CancellationToken ct = default);
}