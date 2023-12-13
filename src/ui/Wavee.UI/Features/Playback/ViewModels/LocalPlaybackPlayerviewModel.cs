using Wavee.UI.Features.Navigation;
using Wavee.UI.Features.Shell.ViewModels;
using Wavee.UI.Test;

namespace Wavee.UI.Features.Playback.ViewModels;

internal sealed class LocalPlaybackPlayerviewModel : PlaybackPlayerViewModel
{
    public LocalPlaybackPlayerviewModel(IUIDispatcher dispatcher,
        INavigationService navigationService,
        RightSidebarLyricsViewModel lyricsRightSidebarViewModel) : base(dispatcher, navigationService, lyricsRightSidebarViewModel)
    {

    }
}