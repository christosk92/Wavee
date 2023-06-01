using ReactiveUI;
using Wavee.Core.Ids;

namespace Wavee.UI.ViewModels.Playlists;

public sealed class PlaylistViewModel : ReactiveObject, INavigableViewModel
{
    public void OnNavigatedTo(object? parameter)
    {
        if (parameter is PlaylistSubscription vr)
        {

        }
        else if (parameter is AudioId id)
        {
            //TODO: fetch the playlist
        }
    }
}