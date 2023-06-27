using Eum.Spotify.connectstate;
using Google.Protobuf.WellKnownTypes;
using LanguageExt;
using Wavee.Id;

namespace Wavee.Remote;

public readonly record struct SpotifyRemoteState(
    Option<SpotifyId> TrackId,
    Option<string> TrackUid,
    Option<string> ContextUri,
    Option<int> IndexInContext,
    TimeSpan Position,
    Option<SpotifyRemoteDeviceInfo> Device)
{
    internal static SpotifyRemoteState? ParseFrom(Option<Cluster> cluster)
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
            TrackId: trackId,
            TrackUid: trackUid,
            ContextUri: contextUri,
            IndexInContext: index,
            Position: CalculatePosition(clusterValue!.PlayerState),
            Device: MutateToDevice(clusterValue)
        );
    }

    private static Option<SpotifyRemoteDeviceInfo> MutateToDevice(Cluster clusterValue)
    {
        if(string.IsNullOrEmpty(clusterValue.ActiveDeviceId))
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

    private static TimeSpan CalculatePosition(PlayerState playerState)
    {
        //todO:
        return TimeSpan.Zero;
    }
}

public readonly record struct SpotifyRemoteDeviceInfo(string Id, string Name, DeviceType Type, double Volume, bool CanControlVolume);