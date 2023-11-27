using System.Collections.Immutable;
using System.Diagnostics;
using Eum.Spotify.connectstate;
using Google.Protobuf.Collections;
using Mediator;
using Wavee.Domain.Playback;
using Wavee.Spotify.Application.Remote.Queries;
using Wavee.Spotify.Domain.Playback;
using Wavee.Spotify.Domain.Remote;
using Wavee.Spotify.Domain.State;

namespace Wavee.Spotify.Application.Remote.QueryHandlers;

public sealed class
    ClusterToPlaybackStateQueryHandler : IRequestHandler<ClusterToPlaybackStateQuery, SpotifyPlaybackState>
{
    private readonly SpotifyClientConfig _config;

    public ClusterToPlaybackStateQueryHandler(SpotifyClientConfig config)
    {
        _config = config;
    }

    public ValueTask<SpotifyPlaybackState> Handle(ClusterToPlaybackStateQuery request,
        CancellationToken cancellationToken)
    {
        var playerState = request.Cluster.PlayerState;
        //If playerState is null there is no active playback session.
        if (playerState == null)
        {
            return ValueTask.FromResult(SpotifyPlaybackState.InActive());
        }

        var contextUri = playerState.ContextUri;
        var contextRestrictions = ParseRestrictions(playerState.Restrictions);
        var playOrigin = ParsePlayOrigin(playerState.PlayOrigin);

        var playbackId = playerState.PlaybackId;
        var sessionId = playerState.SessionId;

        var isPlaying = playerState.IsPlaying;
        var isPaused = playerState.IsPaused;
        var shuffling = playerState.Options.ShufflingContext;
        var repeatState = ParseRepeatState(playerState.Options);

        var timestamp = playerState.Timestamp;
        var positionAsOfTimestamp = playerState.PositionAsOfTimestamp;
        var position = playerState.Position;

        var duration = playerState.Duration;

        var playbackQuality = ParsePlaybackQuality(playerState.PlaybackQuality);

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var elapsed = now - timestamp;
        var offset = TimeSpan.FromMilliseconds(positionAsOfTimestamp + elapsed);

        var otherDevices = request.Cluster.Device
            .Keys
            .Where(x => (x != request.Cluster.ActiveDeviceId && x != _config.Remote.DeviceId))
            .Select(x => ParseDevice(x, request.Cluster.Device))
            .Where(x=> x.HasValue)
            .Select(f=> f!.Value)
            .ToArray();

        return ValueTask.FromResult(new SpotifyPlaybackState(
            IsActive: true,
            Device: ParseDevice(request.Cluster.ActiveDeviceId, request.Cluster.Device),
            OtherDevices: otherDevices,
            Context: new SpotifyPlaybackContext(
                Uri: contextUri,
                Restrictions: contextRestrictions
            ),
            PlayOrigin: playOrigin,
            PlaybackId: playbackId,
            TrackInfo: playerState.Track is not null
                ? new SpotifyPlaybackTrackInfo(
                    Uri: playerState.Track.Uri,
                    Uid: playerState.Track.Uid
                )
                : null,
            SessionId: sessionId,
            IsPaused: isPaused,
            Shuffling: shuffling,
            RepeatState: repeatState
        )
        {
            PositionSw = isPaused ? new Stopwatch() : Stopwatch.StartNew(),
            PositionSwOffset = offset
        });
    }

    private SpotifyDevice? ParseDevice(string clusterActiveDeviceId, MapField<string, DeviceInfo> clusterDevice)
    {
        if (!clusterDevice.ContainsKey(clusterActiveDeviceId))
        {
            return null;
        }

        var deviceInfo = clusterDevice[clusterActiveDeviceId];
        var deviceType = deviceInfo.DeviceType;
        var deviceName = deviceInfo.Name;
        var deviceVolume = deviceInfo.Volume;
        var deviceMetadata = ToDictionary(deviceInfo.Capabilities);

        return new SpotifyDevice(
            Id: clusterActiveDeviceId,
            Type: deviceType,
            Name: deviceName,
            Volume: deviceVolume / SpotifyLocalState.MAX_VOLUME,
            Metadata: deviceMetadata
        );
    }

    private static IReadOnlyDictionary<string, string> ToDictionary(Capabilities deviceInfoCapabilities)
    {
        var fields = Capabilities.Descriptor.Fields.InFieldNumberOrder();
        var dictionary = new Dictionary<string, string>(fields.Count);
        foreach (var field in fields)
        {
            //check if the field is null, if it is, skip it.
            //var hasValue = field.Accessor.HasValue(deviceInfoCapabilities);
            //if (!hasValue) continue;
            var value = field.Accessor.GetValue(deviceInfoCapabilities);
            if (value is null) continue;
            dictionary.Add(field.Name, value.ToString());
        }

        return dictionary;
    }

    private static WaveeRepeatState ParseRepeatState(ContextPlayerOptions playerStateOptions)
    {
        if (playerStateOptions.RepeatingContext) return WaveeRepeatState.Context;
        if (playerStateOptions.RepeatingTrack) return WaveeRepeatState.Track;
        return WaveeRepeatState.None;
    }

    private static SpotifyPlaybackQuality ParsePlaybackQuality(PlaybackQuality playerStatePlaybackQuality)
    {
        return new SpotifyPlaybackQuality();
    }

    private static SpotifyPlaybackOrigin ParsePlayOrigin(PlayOrigin playerStatePlayOrigin)
    {
        var origin = new SpotifyPlaybackOrigin();
        if (!string.IsNullOrEmpty(playerStatePlayOrigin.DeviceIdentifier))
            origin = origin with { DeviceIdentifier = playerStatePlayOrigin.DeviceIdentifier };
        if (!string.IsNullOrEmpty(playerStatePlayOrigin.FeatureIdentifier))
            origin = origin with { FeatureIdentifier = playerStatePlayOrigin.FeatureIdentifier };
        if (!string.IsNullOrEmpty(playerStatePlayOrigin.FeatureVersion))
            origin = origin with { FeatureVersion = playerStatePlayOrigin.FeatureVersion };
        if (!string.IsNullOrEmpty(playerStatePlayOrigin.ViewUri))
            origin = origin with { Referrer = playerStatePlayOrigin.ViewUri };
        if (!string.IsNullOrEmpty(playerStatePlayOrigin.ReferrerIdentifier))
            origin = origin with { Referrer = playerStatePlayOrigin.ReferrerIdentifier };
        if (!string.IsNullOrEmpty(playerStatePlayOrigin.ViewUri))
            origin = origin with { View = playerStatePlayOrigin.ViewUri };
        if (playerStatePlayOrigin.FeatureClasses.Count > 0)
        {
            origin = origin with { FeatureClasses = playerStatePlayOrigin.FeatureClasses.ToArray() };
        }

        return origin;
    }

    private static ImmutableArray<SpotifyPlaybackRestrictionType> ParseRestrictions(
        Restrictions playerStateContextRestrictions)
    {
        var canResume = playerStateContextRestrictions.DisallowResumingReasons.Count is 0;
        var canPause = playerStateContextRestrictions.DisallowPausingReasons.Count is 0;
        var canSeek = playerStateContextRestrictions.DisallowSeekingReasons.Count is 0;
        var canSkipNext = playerStateContextRestrictions.DisallowSkippingNextReasons.Count is 0;
        var canSkipPrevious = playerStateContextRestrictions.DisallowSkippingPrevReasons.Count is 0;
        var canToggleShuffle = playerStateContextRestrictions.DisallowTogglingShuffleReasons.Count is 0;
        var canToggleRepeatTrack = playerStateContextRestrictions.DisallowTogglingRepeatTrackReasons.Count is 0;
        var canToggleRepeatContext = playerStateContextRestrictions.DisallowTogglingRepeatContextReasons.Count is 0;

        var allowedActions = new List<SpotifyPlaybackRestrictionType>(8);
        if (!canResume)
        {
            allowedActions.Add(SpotifyPlaybackRestrictionType.Resume);
        }

        if (!canPause)
        {
            allowedActions.Add(SpotifyPlaybackRestrictionType.Pause);
        }

        if (!canSeek)
        {
            allowedActions.Add(SpotifyPlaybackRestrictionType.Seek);
        }

        if (!canSkipNext)
        {
            allowedActions.Add(SpotifyPlaybackRestrictionType.SkipNext);
        }

        if (!canSkipPrevious)
        {
            allowedActions.Add(SpotifyPlaybackRestrictionType.SkipPrev);
        }

        if (!canToggleShuffle)
        {
            allowedActions.Add(SpotifyPlaybackRestrictionType.Shuffle);
        }

        if (!canToggleRepeatTrack)
        {
            allowedActions.Add(SpotifyPlaybackRestrictionType.RepeatTrack);
        }

        if (!canToggleRepeatContext)
        {
            allowedActions.Add(SpotifyPlaybackRestrictionType.RepeatContext);
        }

        return allowedActions.ToImmutableArray();
    }
}