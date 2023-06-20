using Wavee.Spotify.Common;

namespace Wavee.Spotify.Artist;

public interface IArtistClient
{
    Task<SpotifyArtist> GetArtistAsync(SpotifyId id, CancellationToken cancellationToken = default);

    Task<Paged<SpotifyArtistDiscographyGroup>> GetDiscographyAsync(SpotifyId id,
        DiscographyType type, int offset, int limit, CancellationToken cancellationToken = default);
}