using CommunityToolkit.Mvvm.Input;
using LanguageExt;
using Wavee.UI.WinUI.Components;

namespace Wavee.UI.WinUI.Views;

public interface IPlayableView
{
    AsyncRelayCommand<Option<TrackView>> PlayTrackCommand { get; }
}