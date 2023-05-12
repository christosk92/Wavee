using ReactiveUI;

namespace Wavee.UI.ViewModels.Playlist;

public class PlaylistViewModel : ReactiveObject
{
    private DateTimeOffset _lastPlayedAt;
    private string _name;
    private int _index;
    public string Id { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset LastPlayedAt
    {
        get => _lastPlayedAt;
        set => this.RaiseAndSetIfChanged(ref _lastPlayedAt, value);
    }

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public int Index
    {
        get => _index;
        set => this.RaiseAndSetIfChanged(ref _index, value);
    }
}