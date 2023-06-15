using CommunityToolkit.Mvvm.Input;
using Wavee.UI.WinUI.Components;

namespace Wavee.UI.WinUI.Views;

public interface IPlayableView
{
    AsyncRelayCommand<TrackView> PlayTrackCommand { get; }
}