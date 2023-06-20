using Wavee.Spotify.Common;

namespace Wavee.Spotify.Artist;

public interface IArtistClient
{
    Task<SpotifyArtist> GetArtistAsync(SpotifyId id, CancellationToken cancellationToken = default);
}