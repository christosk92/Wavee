using Wavee.UI.ViewModels.AudioItems;

namespace Wavee.UI.ViewModels.Playback.Impl;

internal class SpotifyPlayerHandler : PlayerViewHandlerInternal
{
    public SpotifyPlayerHandler() : base()
    {
    }

    public override TimeSpan Position
    {
        get;
    }

    public override Task LoadTrackList(IPlayContext context) => throw new NotImplementedException();
    public override void Seek(double position) => throw new NotImplementedException();
}