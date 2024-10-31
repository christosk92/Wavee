using Wavee.Models.Playlist;

namespace Wavee.Interfaces;

public interface ISpotifyPlaylistRepository
{
    Task<IList<SpotifyCachedPlaylistItem>> GetCachedPlaylists();
    Task SaveCachedPlaylists(IList<SpotifyCachedPlaylistItem> playlists);
    Task SavePlaylist(SpotifyCachedPlaylistItem item);
    Task<SpotifyCachedPlaylistItem?> GetPlaylist(string id);
}