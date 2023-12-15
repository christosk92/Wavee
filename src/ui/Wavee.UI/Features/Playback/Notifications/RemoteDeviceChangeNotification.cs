using Wavee.Domain.Playback;

namespace Wavee.UI.Features.Playback.Notifications;

public sealed class RemoteDeviceChangeNotification 
{
    public required IReadOnlyCollection<RemoteDevice> Devices { get; init; }
}
