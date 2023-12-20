using Wavee.UI.Features.Navigation;
using Wavee.UI.Features.RightSidebar.ViewModels;
using Wavee.UI.Features.Shell.ViewModels;
using Wavee.UI.Test;

namespace Wavee.UI.Features.Playback.ViewModels;

internal sealed class LocalPlaybackPlayerviewModel : PlaybackPlayerViewModel
{
    public LocalPlaybackPlayerviewModel(IUIDispatcher dispatcher,
        RightSidebarLyricsViewModel lyricsRightSidebarViewModel) : base(dispatcher, lyricsRightSidebarViewModel)
    {

    }
}