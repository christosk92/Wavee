using System.Collections.ObjectModel;
using ReactiveUI;

namespace Wavee.UI.ViewModels.Playlist;

public sealed class PlaylistViewModel : ReactiveObject
{
    private string _title;
    private int _userIndex;
    private bool _isInFolder;
    public string Uri { get; set; }

    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    public int UserIndex
    {
        get => _userIndex;
        set => this.RaiseAndSetIfChanged(ref _userIndex, value);
    }

    public bool IsInFolder
    {
        get => _isInFolder;
        set => this.RaiseAndSetIfChanged(ref _isInFolder, value);
    }
    public bool IsFolder { get; set; }
    public ObservableCollection<PlaylistViewModel> SubItems { get; }
}