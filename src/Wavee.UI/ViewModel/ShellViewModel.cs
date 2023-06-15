using CommunityToolkit.Mvvm.ComponentModel;
using Wavee.UI.Core;
using Wavee.UI.ViewModel.Library;
using Wavee.UI.ViewModel.Playback;

namespace Wavee.UI.ViewModel;

public sealed class ShellViewModel : ObservableObject
{
    public ShellViewModel(IAppState appState)
    {
        AppState = appState;
        Player = new PlaybackViewModel();
        Libraries = new LibrariesViewModel();
    }
    public LibrariesViewModel Libraries { get; }
    public PlaybackViewModel Player { get; }
    public IAppState AppState { get; }
}