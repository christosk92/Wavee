using Wavee.ViewModels.Models.Items;

namespace Wavee.ViewModels.ViewModels.Common;

public class SongViewModel : AbsWaveeItemViewModel
{
    public SongViewModel(WaveeSongItem item) : base(item)
    {
        
    }
    
    public WaveeSongItem Song => (WaveeSongItem) Item;
}
