using Eum.Spotify.connectstate;
using Google.Protobuf;
using LanguageExt;
using Wavee.Core.Ids;
using static LanguageExt.Prelude;

namespace Wavee.Spotify.Infrastructure.Remote.Contracts;

public readonly record struct SpotifyRemoteState(Option<string> ActiveDeviceId,
    Option<AudioId> TrackUri,
    Option<string> TrackUid,
    bool IsPlaying,
    bool IsPaused,
    bool IsBuffering,
    bool IsShuffling,
    RepeatState RepeatState,
    Option<string> ContextUri,
    TimeSpan Position,
    HashMap<string, SpotifyRemoteDeviceInfo> Devices)
{
    internal static SpotifyRemoteState From(Cluster cluster, string ourDeviceId)
    {
        var playerState = cluster.PlayerState is not null
            ? Some(cluster.PlayerState)
            : Option<PlayerState>.None;
        var uri =
            playerState
                .Bind(t => !string.IsNullOrEmpty(t.Track?.Uri) ? Some(t.Track.Uri) : Option<string>.None)
                .Map(x => AudioId.FromUri(x));

        var trackUid =
            playerState
                .Bind(t => !string.IsNullOrEmpty(t.Track?.Uid) ? Some(t.Track.Uid) : Option<string>.None);

        var repeatState = playerState.Map(x => x.Options.RepeatingTrack
                ? RepeatState.Track
                : (x.Options.RepeatingContext ? RepeatState.Context : RepeatState.None))
            .IfNone(RepeatState.None);

        var contextUri =
            playerState
                .Bind(t => !string.IsNullOrEmpty(t.ContextUri) ? Some(t.ContextUri) : Option<string>.None);

        var activeDeviceId = !string.IsNullOrEmpty(cluster.ActiveDeviceId)
            ? Some(cluster.ActiveDeviceId)
            : Option<string>.None;

        var devices = cluster.Device.ToDictionary(d => d.Key,
                v => new SpotifyRemoteDeviceInfo(
                    DeviceId: v.Value.DeviceId,
                    DeviceName: v.Value.Name,
                    DeviceType: v.Value.DeviceType,
                    Volume: v.Value.Capabilities.DisableVolume
                        ? Option<double>.None
                        : Some((double)v.Value.Volume / ushort.MaxValue)
                )).Where(x => x.Key != ourDeviceId)
            .ToHashMap();

        return new SpotifyRemoteState(
            ActiveDeviceId: activeDeviceId,
            TrackUri: uri,
            TrackUid: trackUid,
            IsPlaying: playerState.Map(t => t.IsPlaying).IfNone(false),
            IsPaused: playerState.Map(t => t.IsPaused).IfNone(false),
            IsBuffering: playerState.Map(t => t.IsBuffering).IfNone(false),
            IsShuffling: playerState.Map(t => t.Options.ShufflingContext).IfNone(false),
            RepeatState: repeatState,
            ContextUri: contextUri,
            Position: ParsePosition(playerState),
            devices
        );
    }

    private static TimeSpan ParsePosition(Option<PlayerState> playerState)
    {
        return playerState.Map(p =>
        {
            var isPaused = p.IsPaused;
            var position = p.Position;
            if (!isPaused)
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var timestamp = p.Timestamp;
                var positionAsOfTimestamp = p.PositionAsOfTimestamp;
                var diff = now - timestamp;
                var newPosition = positionAsOfTimestamp + diff;
                return TimeSpan.FromMilliseconds(Math.Max(0, newPosition));
            }

            return TimeSpan.FromMilliseconds(p.PositionAsOfTimestamp);
        }).IfNone(TimeSpan.Zero);
    }
}

public readonly record struct SpotifyRemoteDeviceInfo(
    string DeviceId,
    string DeviceName,
    DeviceType DeviceType,
    Option<double> Volume);