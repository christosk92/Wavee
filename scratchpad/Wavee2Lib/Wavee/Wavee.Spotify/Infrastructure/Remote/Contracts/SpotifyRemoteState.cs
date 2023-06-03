using Eum.Spotify.connectstate;
using Eum.Spotify.context;
using Google.Protobuf;
using Google.Protobuf.Collections;
using LanguageExt;
using LanguageExt.SomeHelp;
using Wavee.Core.Ids;
using static LanguageExt.Prelude;

namespace Wavee.Spotify.Infrastructure.Remote.Contracts;

public readonly record struct SpotifyRemoteState(Option<string> ActiveDeviceId,
    Option<AudioId> TrackUri,
    Option<string> TrackUid,
    Option<int> TrackIndex,
    Option<ProvidedTrack> PlayingFromQueue,
    Option<ProvidedTrack> TrackForQueueHint,
    bool IsPlaying,
    bool IsPaused,
    bool IsBuffering,
    bool IsShuffling,
    RepeatState RepeatState,
    Option<string> ContextUri,
    TimeSpan Position,
    IEnumerable<ProvidedTrack> NextTracks,
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

        var trackidx =
            playerState
                .Bind(t => t.Index is not null ? Some(t.Index.Track) : Option<uint>.None);


        var trackUid =
            playerState
                .Bind(t => !string.IsNullOrEmpty(t.Track?.Uid) ? Some(t.Track.Uid) : Option<string>.None);

        var trackFromQueue =
            playerState.Bind(x => x.Track is not null && x.Track.Provider is "queue" ? Some(x.Track) : None);

        var firstRealTrackAfterQueueForHint = playerState.Bind(x => x.NextTracks.SkipWhile(y => y.Provider is "queue"))
            .FirstOrDefault() is { } track
            ? Some(track)
            : None;

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

        var nextTracks = playerState.Map(x => x.NextTracks.AsEnumerable())
            .IfNone(Enumerable.Empty<ProvidedTrack>());

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
            TrackIndex: trackidx.Map(x => (int)x),
            PlayingFromQueue: trackFromQueue,
            TrackForQueueHint: firstRealTrackAfterQueueForHint,
            IsPlaying: playerState.Map(t => t.IsPlaying).IfNone(false),
            IsPaused: playerState.Map(t => t.IsPaused).IfNone(false),
            IsBuffering: playerState.Map(t => t.IsBuffering).IfNone(false),
            IsShuffling: playerState.Map(t => t.Options.ShufflingContext).IfNone(false),
            RepeatState: repeatState,
            ContextUri: contextUri,
            Position: ParsePosition(playerState),
            NextTracks: nextTracks,
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