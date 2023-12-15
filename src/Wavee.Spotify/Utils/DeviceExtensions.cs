using Wavee.Domain.Playback;
using Wavee.Spotify.Domain.Remote;

namespace Wavee.Spotify.Utils;

public static class DeviceExtensions
{
    public static RemoteDevice ToRemoteDevice(this SpotifyDevice device)
    {
        return new RemoteDevice
        {
            Id = device.Id,
            Type = (RemoteDeviceType)(int)device.Type,
            Name = device.Name,
            Volume = device.Volume,
            Metadata = device.Metadata
        };
    }
}