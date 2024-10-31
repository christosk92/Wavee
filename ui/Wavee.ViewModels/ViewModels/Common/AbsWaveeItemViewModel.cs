using Wavee.ViewModels.Models;

namespace Wavee.ViewModels.ViewModels.Common;

public abstract class AbsWaveeItemViewModel
{
    protected AbsWaveeItemViewModel(WaveeItem item)
    {
        Item = item;
    }
    
    public WaveeItem Item { get; }
}