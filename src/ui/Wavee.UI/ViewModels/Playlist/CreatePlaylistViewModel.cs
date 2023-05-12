using System.Windows.Input;
using ReactiveUI;

namespace Wavee.UI.ViewModels.Playlist;

public sealed class CreatePlaylistViewModel : ReactiveObject
{
    private string _playlistName;
    private bool _createInSpotify;
    private string? _imageSource;
    public IObservable<bool> HasProfileForSomething => MainViewModel.Instance.UserIsLoggedIn;

    public string PlaylistName
    {
        get => _playlistName;
        set => this.RaiseAndSetIfChanged(ref _playlistName, value);
    }

    public string? ImageSource
    {
        get => _imageSource;
        set => this.RaiseAndSetIfChanged(ref _imageSource, value);
    }

    public bool CreateInSpotify
    {
        get => _createInSpotify;
        set => this.RaiseAndSetIfChanged(ref _createInSpotify, value);
    }

    public ICommand CreateCommand { get; }
}