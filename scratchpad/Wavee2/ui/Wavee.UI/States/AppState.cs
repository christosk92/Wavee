using ReactiveUI;
using Wavee.Spotify;
using Wavee.UI.Config;
using Wavee.UI.States.Spotify;
using Wavee.UI.ViewModels;

namespace Wavee.UI.States;

public class AppState : ReactiveObject
{
    private User _currentUser;

    public AppState(AppConfig config)
    {
        Config = config;
        SpotifyState = new SpotifyState(config);
        Instance = this;
    }

    public SpotifyState SpotifyState { get; }

    public AppConfig Config { get; }

    public User CurrentUser
    {
        get => _currentUser;
        set => this.RaiseAndSetIfChanged(ref _currentUser, value);
    }
    public static AppState Instance { get; private set; }
}