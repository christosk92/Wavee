using Eum.UI.Items;
using Eum.UI.Playlists;
using Eum.UI.Users;

namespace Eum.UI.Services.Playlists;

public interface IEumPlaylistManager
{
    ValueTask<IEnumerable<EumPlaylist>> GetPlaylists(ItemId userId, bool refreshList);

    ValueTask<EumPlaylist> AddPlaylist(string name,
        string id,
        string? picture,
        ServiceType serviceType,
        ItemId forUser, Dictionary<ServiceType, ItemId> linkedWith);

    void AddPlaylist(EumPlaylist playlist);

    event EventHandler<EumPlaylist>? PlaylistUpdated;
    event EventHandler<EumPlaylist> PlaylistAdded;
    event EventHandler<EumPlaylist> PlaylistRemoved;

    void RemovePlaylist(EumPlaylist playlists);
}