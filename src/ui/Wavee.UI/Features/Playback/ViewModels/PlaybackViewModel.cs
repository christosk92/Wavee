using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Mediator;
using Wavee.Domain.Playback;
using Wavee.Spotify.Common.Contracts;
using Wavee.UI.Domain.Playback;
using Wavee.UI.Features.Album.ViewModels;
using Wavee.UI.Features.Artist.Queries;
using Wavee.UI.Features.Navigation;
using Wavee.UI.Features.Playback.Notifications;

namespace Wavee.UI.Features.Playback.ViewModels;

public sealed class PlaybackViewModel : ObservableObject,
    INotificationHandler<PlaybackPlayerChangedNotification>
{
    private readonly IMediator _mediator;
    private readonly INavigationService _navigationService;
    private PlaybackPlayerViewModel? _activePlayer;
    private readonly ISpotifyClient _spotifyClient;
    public PlaybackViewModel(IMediator mediator, ISpotifyClient spotifyClient, INavigationService navigationService)
    {
        _mediator = mediator;
        _spotifyClient = spotifyClient;
        _navigationService = navigationService;
    }

    public PlaybackPlayerViewModel? ActivePlayer
    {
        get => _activePlayer;
        set
        {
            if (SetProperty(ref _activePlayer, value))
            {
                value?.Activate();
            }
        }
    }
    public event EventHandler PlaybackStateChanged;
    public ObservableCollection<PlaybackPlayerViewModel> Players { get; } = new();

    public ValueTask Handle(PlaybackPlayerChangedNotification notification, CancellationToken cancellationToken)
    {
        if (ActivePlayer is not null)
        {
            Players.Add(ActivePlayer);
            ActivePlayer.PlaybackChanged -= ActivePlayerOnPlaybackChanged;
            ActivePlayer?.Dispose();
        }


        ActivePlayer = notification.Player;
        ActivePlayer.PlaybackChanged += ActivePlayerOnPlaybackChanged;
        Players.Add(notification.Player);
        return ValueTask.CompletedTask;
    }

    private void ActivePlayerOnPlaybackChanged(object? sender, EventArgs e)
    {
        PlaybackStateChanged?.Invoke(this, EventArgs.Empty);

        if (_navigationService.CurrentViewModel is IPlaybackChangedListener listener)
        {
            listener.OnPlaybackChanged(this);
        }
    }

    public async Task Play(PlayContext ctx)
    {
        if (ActivePlayer is SpotifyRemotePlaybackPlayerViewModel)
        {
            // Do command
            await _spotifyClient.Remote.Play(ctx._spContext, ctx._playOrigin, ctx._playOptions);
        }
    }

    public WaveeTrackPlaybackState IsPlaying(string id, string? uid)
    {
        if (_activePlayer is null) 
            return WaveeTrackPlaybackState.NotPlaying;

        bool isplayingTrack = false;
        if (!string.IsNullOrEmpty(uid))
        {
            //TODO:
            isplayingTrack = false;
        }
        else
        {
            var state = ActivePlayer?.Id;
            isplayingTrack = state == id;
        }

        if (isplayingTrack)
        {
            return _activePlayer.IsPaused ? WaveeTrackPlaybackState.Paused : WaveeTrackPlaybackState.Playing;
        }

        return WaveeTrackPlaybackState.NotPlaying;
    }
}
