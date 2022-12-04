using System.Collections.ObjectModel;
using DynamicData;
using Eum.UI.Playlists;
using Eum.UI.Users;
using Eum.UI.ViewModels.Playlists;

namespace Eum.UI.Services.Playlists
{
    public interface IEumUserPlaylistViewModelManager
    {
        Task<PlaylistViewModel> WaitForVm(EumPlaylist playlist);
        SourceList<PlaylistViewModel> SourceList { get; }
        ObservableCollection<PlaylistViewModel> Playlists { get; }
    }
}
