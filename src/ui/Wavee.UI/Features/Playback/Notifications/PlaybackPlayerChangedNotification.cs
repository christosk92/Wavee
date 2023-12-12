using Mediator;
using Wavee.UI.Features.Playback.ViewModels;

namespace Wavee.UI.Features.Playback.Notifications;

public sealed class PlaybackPlayerChangedNotification : INotification
{
    public required PlaybackPlayerViewModel Player { get; init; }
}