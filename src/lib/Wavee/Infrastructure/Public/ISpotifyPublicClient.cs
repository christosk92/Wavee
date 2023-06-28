using LanguageExt;
using Wavee.Id;
using Wavee.Infrastructure.Public.Response;

namespace Wavee.Infrastructure.Public;

public interface ISpotifyPublicClient
{
    Task<PagedResponse<SpotifyPublicTrack>> GetMyTracks(int offset, int limit, CancellationToken ct = default);
    Task<PagedResponse<SpotifyPublicTrack>> GetAlbumTracks(SpotifyId albumId, int offset, int limit, CancellationToken ct = default);
    Task<PagedResponse<ISpotifyPlaylistItem>> GetPlaylistTracks(SpotifyId spotifyId, int offset, int limit, Option<AudioItemType[]> types, CancellationToken ct = default);
    Task<SpotifyPublicTrack> GetTrack(SpotifyId spotifyId, CancellationToken ct = default);
    Task<IReadOnlyCollection<SpotifyPublicTrack>> GetArtistTopTracks(SpotifyId spotifyId, string us, CancellationToken ct = default);
}