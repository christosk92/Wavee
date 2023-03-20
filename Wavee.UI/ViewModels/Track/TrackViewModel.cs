using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wavee.Interfaces.Models;
using Wavee.UI.Interfaces.Playback;
using Wavee.UI.Playback.Contexts;

namespace Wavee.UI.ViewModels.Track;

[INotifyPropertyChanged]
public partial class TrackViewModel : IComparable<TrackViewModel>
{
    [ObservableProperty]
    private int _index;

    public TrackViewModel(ITrack track,
        int index)
    {
        _index = index;
        Track = track;
        OriginalIndex = index;
    }

    public ITrack Track { get; }
    public int OriginalIndex { get; }



    public int CompareTo(TrackViewModel? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return _index.CompareTo(other._index);
    }
}
