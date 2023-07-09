using Eum.Spotify.connectstate;
using Google.Protobuf.WellKnownTypes;
using LanguageExt;
using Wavee.Id;
using Wavee.Time;

namespace Wavee.Remote;

public readonly record struct SpotifyRemoteState(
    bool Paused,
    bool HasPlayback,
    Option<SpotifyId> TrackId,
    Option<string> TrackUid,
    Option<string> ContextUri,
    Option<int> IndexInContext,
    TimeSpan Position,
    Option<SpotifyRemoteDeviceInfo> Device,
    IEnumerable<SpotifyRemoteDeviceInfo> Devices)
{
    internal static SpotifyRemoteState? ParseFrom(Option<Cluster> cluster, ITimeProvider client, string ourDeviceId)
    {
        if (cluster.IsNone)
            return null;

        var clusterValue = cluster.IfNoneUnsafe(() => throw new InvalidOperationException());
        var trackUri = clusterValue!.PlayerState?.Track?.Uri;
        var trackId = !string.IsNullOrEmpty(trackUri) ? SpotifyId.FromUri(trackUri.AsSpan()) : Option<SpotifyId>.None;
        var trackUidStr = clusterValue!.PlayerState?.Track?.Uid;
        var trackUid = !string.IsNullOrEmpty(trackUidStr) ? trackUidStr : Option<string>.None;

        var contextUriStr = clusterValue!.PlayerState?.ContextUri;
        var contextUri = !string.IsNullOrEmpty(contextUriStr) ? contextUriStr : Option<string>.None;
        var index = clusterValue!.PlayerState?.Index?.Track is not null ? (int)clusterValue!.PlayerState.Index.Track : Option<int>.None;
        return new SpotifyRemoteState(
            Paused: clusterValue!.PlayerState?.IsPaused is true,
            HasPlayback: clusterValue!.PlayerState?.IsPlaying is true,
            TrackId: trackId,
            TrackUid: trackUid,
            ContextUri: contextUri,
            IndexInContext: index,
            Position: CalculatePosition(clusterValue!.PlayerState, client.CurrentTimeMilliseconds, client.Offset),
            Device: MutateToDevice(clusterValue),
            Devices: clusterValue.Device
                .Where(x=> x.Key != ourDeviceId)
                .Select(x=> MutateToDevice(x.Value))
        );
    }

    private static SpotifyRemoteDeviceInfo MutateToDevice(DeviceInfo dv)
    {
        return new SpotifyRemoteDeviceInfo(
            Id: dv.DeviceId,
            Name: dv.Name,
            Type: dv.DeviceType,
            Volume: dv.Capabilities.VolumeSteps is 0 ? 1 : (double)dv.Volume / dv.Capabilities.VolumeSteps,
            CanControlVolume: !dv.Capabilities.DisableVolume
        );
    }

    private static Option<SpotifyRemoteDeviceInfo> MutateToDevice(Cluster clusterValue)
    {
        if (string.IsNullOrEmpty(clusterValue.ActiveDeviceId))
            return Option<SpotifyRemoteDeviceInfo>.None;

        if (clusterValue.Device.TryGetValue(clusterValue.ActiveDeviceId, out var dv))
        {
            return new SpotifyRemoteDeviceInfo(
                Id: dv.DeviceId,
                Name: dv.Name,
                Type: dv.DeviceType,
                Volume: dv.Capabilities.VolumeSteps is 0 ? 1 : (double)dv.Volume / dv.Capabilities.VolumeSteps,
                CanControlVolume: !dv.Capabilities.DisableVolume
            );
        }

        return Option<SpotifyRemoteDeviceInfo>.None;
    }

    private static TimeSpan CalculatePosition(PlayerState? playerState, long serverTime, int serverOffset)
    {
        if(playerState is null) return TimeSpan.Zero;
        
        if (playerState.IsPaused)
        {
            return TimeSpan.FromMilliseconds(playerState.PositionAsOfTimestamp + serverOffset);
        }
        var diff = serverTime - playerState.Timestamp;
        return TimeSpan.FromMilliseconds(playerState.PositionAsOfTimestamp + diff);
    }
}

public readonly record struct SpotifyRemoteDeviceInfo(string Id, string Name, DeviceType Type, double Volume, bool CanControlVolume);