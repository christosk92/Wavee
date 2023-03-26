using Wavee.Interfaces.Models;

namespace Wavee.UI.Interfaces.Playback
{
    public interface IPlayContext : IEquatable<IPlayContext>
    {
        IPlayableItem? GetTrack(int index);
        int Length
        {
            get;
        }
    }
}
