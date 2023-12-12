using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Mediator;
using Wavee.UI.Features.Playback.Notifications;

namespace Wavee.UI.Features.Playback.ViewModels;

public sealed class PlaybackViewModel : ObservableObject,
    INotificationHandler<PlaybackPlayerChangedNotification>
{
    private readonly IMediator _mediator;
    private PlaybackPlayerViewModel? _activePlayer;

    public PlaybackViewModel(IMediator mediator)
    {
        _mediator = mediator;
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
}