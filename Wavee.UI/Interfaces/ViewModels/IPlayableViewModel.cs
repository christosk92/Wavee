using CommunityToolkit.Mvvm.Input;
using Wavee.UI.ViewModels.Track;

namespace Wavee.UI.Interfaces.ViewModels;
public interface IPlayableViewModel
{
    AsyncRelayCommand<TrackViewModel> PlayCommand
    {
        get;
    }
}
