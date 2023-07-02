using Wavee.UI.Client.Playlist.Models;

namespace Wavee.UI.Client.Playlist;

public interface IWaveeUIPlaylistClient
{
    ValueTask<WaveeUIPlaylist> GetPlaylist(string id, CancellationToken cancellationToken);
}