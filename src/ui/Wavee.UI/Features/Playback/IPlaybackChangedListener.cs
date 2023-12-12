using Wavee.UI.Features.Playback.ViewModels;

namespace Wavee.UI.Features.Playback;

partial interface IPlaybackChangedListener
{
    void OnPlaybackChanged(PlaybackViewModel player);
}