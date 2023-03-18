using CommunityToolkit.Mvvm.Input;
using Wavee.Interfaces.Models;
using Wavee.UI.Interfaces.Playback;

namespace Wavee.UI.ViewModels.Track
{
    public sealed class LibraryTrackViewModel : TrackViewModel
    {
        public LibraryTrackViewModel(ITrack track,
            int index,
            string extraGroupString,
            IAsyncRelayCommand<IPlayContext?> playCommand) : base(track, index, playCommand)
        {
            ExtraGroup = extraGroupString;
        }

        public string ExtraGroup { get; }
    }
}
