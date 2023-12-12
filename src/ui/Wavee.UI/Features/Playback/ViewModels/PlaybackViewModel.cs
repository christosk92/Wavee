using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Mediator;
using Wavee.Spotify.Common.Contracts;
using Wavee.UI.Domain.Playback;
using Wavee.UI.Features.Playback.Notifications;

namespace Wavee.UI.Features.Playback.ViewModels;

public sealed class PlaybackViewModel : ObservableObject,
    INotificationHandler<PlaybackPlayerChangedNotification>
{
    private readonly IMediator _mediator;
    private PlaybackPlayerViewModel? _activePlayer;
    private readonly ISpotifyClient _spotifyClient;
    public PlaybackViewModel(IMediator mediator, ISpotifyClient spotifyClient)
    {
        _mediator = mediator;
        _spotifyClient = spotifyClient;
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
    public ObservableCollection<PlaybackPlayerViewModel> Players { get; } = new();

    public ValueTask Handle(PlaybackPlayerChangedNotification notification, CancellationToken cancellationToken)
    {
        if (ActivePlayer is not null)
        {
            Players.Add(ActivePlayer);
            ActivePlayer?.Dispose();
        }


        ActivePlayer = notification.Player;
        Players.Add(notification.Player);
        return ValueTask.CompletedTask;
    }

    public async Task Play(PlayContext ctx)
    {
        if (ActivePlayer is SpotifyRemotePlaybackPlayerViewModel)
        {
            // Do command
            await _spotifyClient.Remote.Play(ctx._spContext, ctx._playOrigin, ctx._playOptions);
        }
    }
}
