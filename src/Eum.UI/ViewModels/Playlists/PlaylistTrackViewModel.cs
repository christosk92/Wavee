using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eum.UI.Services.Tracks;

namespace Eum.UI.ViewModels.Playlists
{
    public class PlaylistTrackViewModel
    {
        public PlaylistTrackViewModel(EumTrack eumTrack, int index)
        {
            Index = index;
            Track = eumTrack;
        }

        public int Index { get; }
        public EumTrack Track { get; }
    }
}
