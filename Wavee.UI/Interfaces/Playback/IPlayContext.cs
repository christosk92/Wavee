using Wavee.Interfaces.Models;

namespace Wavee.UI.Interfaces.Playback
{
    public interface IPlayContext
    {
        IPlayableItem? GetTrack(int index);
        int Length
        {
            get;
        }
    }
}
