using Eum.Spotify.connectstate;
using Wavee.Spotify.Common;

namespace Wavee.Spotify.Remote;

public readonly record struct SpotifyRemoteState(
    SpotifyId? TrackId,
    string? TrackUid,
    ProvidedTrack? PlayingFromQueue,
    bool IsPlaying,
    bool IsPaused,
    bool IsBuffering,
    bool IsShuffling,
    RepeatState RepeatState,
    string? ContextUri,
    uint? IndexInContext,
    TimeSpan Position,
    IEnumerable<ProvidedTrack> NextTracks,
    IReadOnlyDictionary<string, SpotifyRemoteDeviceInfo> Devices)
{
    internal static SpotifyRemoteState ParseFrom(Cluster cluster, string ourDeviceId)
    {
        var playerState = cluster.PlayerState;

        SpotifyId? trackId = null;
        string? trackUid = null;
        ProvidedTrack? trackFromQueue = null;
        if (!string.IsNullOrEmpty(playerState?.Track?.Uri))
        {
            trackId = SpotifyId.FromUri(playerState.Track.Uri);
        }

        if (!string.IsNullOrEmpty(playerState?.Track?.Uid))
        {
            trackUid = playerState.Track.Uid;
        }

        if (playerState?.Track is not null && playerState.Track.Provider is "queue")
        {
            trackFromQueue = playerState.Track;
        }

        var trackIdx = playerState?.Index?.Track;
        var repeatState = playerState?.Options?.RepeatingTrack is true
            ? Wavee.RepeatState.Track
            : (playerState?.Options?.RepeatingContext is true
                ? Wavee.RepeatState.Context
                : Wavee.RepeatState.None);

        var contextUri = playerState?.ContextUri;
        var activeDeviceId = cluster.ActiveDeviceId;

        var nextTracks = playerState?.NextTracks?.AsEnumerable() ?? Enumerable.Empty<ProvidedTrack>();

        var devices = cluster.Device.ToDictionary(d => d.Key,
                v => new SpotifyRemoteDeviceInfo(
                    DeviceId: v.Value.DeviceId,
                    DeviceName: v.Value.Name,
                    DeviceType: v.Value.DeviceType,
                    IsActive: activeDeviceId == v.Value.DeviceId,
                    Volume: v.Value.Capabilities.DisableVolume
                        ? null
                        : ((double)v.Value.Volume / ushort.MaxValue)
                )).Where(x => x.Key != ourDeviceId)
            .ToDictionary();

        return new SpotifyRemoteState(
            TrackId: trackId,
            TrackUid: trackUid,
            PlayingFromQueue: trackFromQueue,
            IsPlaying: playerState?.IsPlaying is true,
            IsPaused: playerState?.IsPaused is true,
            IsBuffering: playerState?.IsBuffering is true,
            IsShuffling: playerState?.Options?.ShufflingContext is true,
            RepeatState: repeatState,
            ContextUri: contextUri,
            IndexInContext: trackIdx,
            Position: ParsePosition(playerState),
            NextTracks: nextTracks,
            Devices: devices
        );
    }

    private static TimeSpan ParsePosition(PlayerState? playerState)
    {
        if (playerState is null)
        {
            return TimeSpan.Zero;
        }

        var isPaused = playerState.IsPaused;
        var position = playerState.Position;
        if (!isPaused)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var timestamp = playerState.Timestamp;
            var positionAsOfTimestamp = playerState.PositionAsOfTimestamp;
            var diff = now - timestamp;
            var newPosition = positionAsOfTimestamp + diff;
            return TimeSpan.FromMilliseconds(Math.Max(0, newPosition));
        }

        return TimeSpan.FromMilliseconds(playerState.PositionAsOfTimestamp);
    }
}

public readonly record struct SpotifyRemoteDeviceInfo(
    string DeviceId,
    string DeviceName,
    DeviceType DeviceType,
    bool IsActive,
    double? Volume)
{
    public bool CanControlVolume => Volume.HasValue;
}