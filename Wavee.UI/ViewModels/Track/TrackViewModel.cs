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
        int index,
        IAsyncRelayCommand<IPlayContext?> playCommand)
    {
        _index = index;
        Track = track;
        OriginalIndex = index;
        PlayCommand = playCommand;
    }

    public ITrack Track { get; }
    public int OriginalIndex { get; }

    public IAsyncRelayCommand<IPlayContext> PlayCommand { get; }

    public IPlayContext Context
    {
        get
        {
            //the database will prepend SELECT Id, LastChanged  FROM MediaItems
            //and append LIMIT 1
            //so all we need to do is add a sort and skip to the correct index
            //var sortSql = $"ORDER BY DateImported DESC OFFSET {Index}";
            var context = new LocalFilesContext("ORDER BY DateImported DESC", Index, null);
            return context;
        }
    }

    public int CompareTo(TrackViewModel? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return _index.CompareTo(other._index);
    }
}
