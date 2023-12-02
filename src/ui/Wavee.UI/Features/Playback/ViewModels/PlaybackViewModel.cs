using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Mediator;
using Wavee.UI.Features.Playback.Notifications;

namespace Wavee.UI.Features.Playback.ViewModels;

public sealed class PlaybackViewModel : ObservableObject, 
    INotificationHandler<PlaybackPlayerAddedNotification>,
    INotificationHandler<PlaybackPlayerRemovedNotification>
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
    public ValueTask Handle(PlaybackPlayerAddedNotification notification, CancellationToken cancellationToken)
    {
        Players.Add(notification.Player);
        ActivePlayer = Players.FirstOrDefault(x => x.IsActive) ?? Players.FirstOrDefault();
        return ValueTask.CompletedTask;
    }

    public ValueTask Handle(PlaybackPlayerRemovedNotification notification, CancellationToken cancellationToken)
    {
        Players.Remove(notification.Player);
        ActivePlayer = Players.FirstOrDefault(x => x.IsActive) ?? Players.FirstOrDefault();
        return ValueTask.CompletedTask;
    }
}