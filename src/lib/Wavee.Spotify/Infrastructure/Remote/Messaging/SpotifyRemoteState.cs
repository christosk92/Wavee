using Eum.Spotify.connectstate;
using LanguageExt;
using System.Linq;
using Wavee.Core.Ids;
using Wavee.Core.Playback;
using Wavee.Spotify.Helpers;

namespace Wavee.Spotify.Infrastructure.Remote.Messaging;

public readonly record struct SpotifyRemoteState(
    Option<string> ActiveDeviceId,
    Option<AudioId> TrackUri,
    Option<string> TrackUid,
    bool IsPlaying,
    bool IsPaused,
    bool IsBuffering,
    bool IsShuffling,
    RepeatState RepeatState,
    Option<AudioId> ContextUri,
    TimeSpan Position,
    HashMap<string, SpotifyRemoteDeviceInfo> Devices)
{
    internal static SpotifyRemoteState From(
        string ourDeviceId,
        Option<Cluster> cluster)
    {
        var playerState = cluster.Bind(c => c.PlayerState is not null
            ? Some(c.PlayerState)
            : Option<PlayerState>.None);
        var uri =
            playerState
                .Bind(t => !string.IsNullOrEmpty(t.Track?.Uri) ? Some(t.Track.Uri) : Option<string>.None)
                .Map(x => AudioId.FromUri(x));

        var trackUid =
            playerState
                .Bind(t => !string.IsNullOrEmpty(t.Track?.Uid) ? Some(t.Track.Uid) : Option<string>.None);

        var repeatState = playerState.Map(x => x.Options.RepeatingTrack
                ? RepeatState.Track : (x.Options.RepeatingContext ? RepeatState.Context: RepeatState.None))
            .IfNone(Core.Playback.RepeatState.None);

        var contextUri =
            playerState
                .Bind(t => !string.IsNullOrEmpty(t.ContextUri) ? Some(t.ContextUri) : Option<string>.None)
                .Map(x => AudioId.FromUri(x));

        var activeDeviceId = cluster.Bind(c => !string.IsNullOrEmpty(c.ActiveDeviceId)
            ? Some(c.ActiveDeviceId)
            : Option<string>.None);

        var devices = cluster.Map(c => c.Device.ToDictionary(d => d.Key,
            v => new SpotifyRemoteDeviceInfo(
                DeviceId: v.Value.DeviceId,
                DeviceName: v.Value.Name,
                DeviceType: v.Value.DeviceType,
                Volume: v.Value.Capabilities.DisableVolume
                    ? Option<double>.None
                    : Some((double)v.Value.Volume / ushort.MaxValue)
            )).Where(x => x.Key != ourDeviceId).ToHashMap())
            .IfNone(LanguageExt.HashMap<string, SpotifyRemoteDeviceInfo>.Empty);

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