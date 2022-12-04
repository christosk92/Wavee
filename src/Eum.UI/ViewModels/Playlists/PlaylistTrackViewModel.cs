using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eum.UI.ViewModels.Playlists
{
    public class PlaylistTrackViewModel
    {
        public PlaylistTrackViewModel(int index)
        {
            Index = index;
        }

        public int Index { get; }
    }
}
