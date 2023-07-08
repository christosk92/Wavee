using Eum.Spotify.playlist4;
using Wavee.UI.Client.Playlist.Models;
using Wavee.UI.ViewModel.Playlist.User;

namespace Wavee.UI.Client.Playlist;

public interface IWaveeUIPlaylistClient
{
    ValueTask<WaveeUIPlaylist> GetPlaylist(string id, CancellationToken cancellationToken);
    IObservable<PlaylistInfoNotification> ListenForUserPlaylists();
}