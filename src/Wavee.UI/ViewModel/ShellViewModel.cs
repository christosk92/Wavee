using CommunityToolkit.Mvvm.ComponentModel;
using LanguageExt;
using Wavee.Core.Ids;
using Wavee.UI.Core;
using Wavee.UI.ViewModel.Library;
using Wavee.UI.ViewModel.Playback;
using Wavee.UI.ViewModel.Search;

namespace Wavee.UI.ViewModel;

public sealed class ShellViewModel : ObservableObject
{
    public ShellViewModel(IAppState appState)
    {
        AppState = appState;
        Search = new SearchViewModel(appState);
        Libraries = new LibrariesViewModel(appState);
        Player = new PlaybackViewModel();
    }
    public SearchViewModel Search { get; }
    public LibrariesViewModel Libraries { get; }
    public PlaybackViewModel Player { get; }
    public IAppState AppState { get; }
}