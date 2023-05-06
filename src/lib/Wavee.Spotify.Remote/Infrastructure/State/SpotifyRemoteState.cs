using Eum.Spotify.connectstate;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Spotify.Remote.Infrastructure.State.Messages;
using ContextPlayerOptions = Eum.Spotify.connectstate.ContextPlayerOptions;
using PlayOrigin = Eum.Spotify.connectstate.PlayOrigin;
using Restrictions = Eum.Spotify.connectstate.Restrictions;

namespace Wavee.Spotify.Remote.Infrastructure.State;

internal readonly record struct RemoteStateTimeData(
    long Timestamp,
    long ServerTime,
    Option<ulong> StartedPlayingAt)
{
    public static RemoteStateTimeData FromCluster(Cluster cluster)
    {
        return new RemoteStateTimeData(
            Timestamp: cluster.Timestamp,
            ServerTime: cluster.ServerTimestampMs,
            StartedPlayingAt: None);
    }
}

internal readonly record struct RemoteStateContextData(
    string Uri,
    HashMap<RestrictionAction, Seq<string>> Restrictions,
    HashMap<string, string> Metadata)
{
    public static RemoteStateContextData FromCluster(Cluster cluster)
    {
        return new RemoteStateContextData();
    }
}

internal enum RestrictionAction
{
    SHUFFLE,
    REPEAT_CONTEXT,
    REPEAT_TRACK,
    PAUSE,
    RESUME,
    SEEK,
    SKIP_PREV,
    SKIP_NEXT
}

internal readonly record struct RemoteStatePlaybackData(
    long Timestamp,
    long PositionAsOfTimestamp,
    PlayOrigin PlayOrigin,
    Option<uint> Index,
    Option<ProvidedTrack> Track,
    Option<string> PlaybackId,
    long Duration,
    bool IsPaused,
    bool IsPlaying,
    bool IsShuffling,
    bool RepeatTrack,
    bool RepeatContext,
    HashMap<RestrictionAction, Seq<string>> Restrictions,
    Option<string> SessionId)
{
    public static RemoteStatePlaybackData FromCluster(Cluster cluster)
    {
        return new RemoteStatePlaybackData(
            Timestamp: cluster.PlayerState.Timestamp,
            PositionAsOfTimestamp: cluster.PlayerState.PositionAsOfTimestamp,
            PlayOrigin: cluster.PlayerState.PlayOrigin,
            Index: cluster.PlayerState.Index?.Track is { } index ? Some(index) : None,
            Track: cluster.PlayerState.Track,
            PlaybackId: cluster.PlayerState.PlaybackId,
            Duration: cluster.PlayerState.Duration,
            IsPaused: cluster.PlayerState.IsPaused,
            IsPlaying: cluster.PlayerState.IsPlaying,
            IsShuffling: cluster.PlayerState.Options.ShufflingContext,
            RepeatTrack: cluster.PlayerState.Options.RepeatingTrack,
            RepeatContext: cluster.PlayerState.Options.RepeatingContext,
            Restrictions: LanguageExt.HashMap<RestrictionAction, Seq<string>>.Empty,
            SessionId: cluster.PlayerState.SessionId
        );
    }
}

internal readonly record struct RemoteStateTimelineData(
    Seq<ProvidedTrack> NextTracks,
    Seq<ProvidedTrack> PrevTracks)
{
    public static RemoteStateTimelineData FromCluster(Cluster cluster)
    {
        return new RemoteStateTimelineData(
            NextTracks: cluster.PlayerState.NextTracks.ToSeq(),
            PrevTracks: cluster.PlayerState.PrevTracks.ToSeq()
        );
    }
}

internal readonly record struct LastCommandData(string LastCommandSentByDeviceId, uint LastMessageId);

internal readonly record struct SpotifyRemoteState(
    Option<RemoteStateTimeData> Time,
    Option<RemoteStateContextData> Context,
    Option<RemoteStatePlaybackData> Playback,
    Option<RemoteStateTimelineData> Timeline,
    Option<LastCommandData> LastCommand)
{
    public SpotifyRemoteState() : this(None, None, None, None, None)
    {
    }

    public string ConnectionId { get; init; }
    public string DeviceName { get; init; }
    public string DeviceId { get; init; }
    public DeviceType DeviceType { get; init; }

    public SpotifyRemoteState FromCluster(Cluster cluster)
    {
        var timedata = RemoteStateTimeData.FromCluster(cluster);
        var contextdata = RemoteStateContextData.FromCluster(cluster);
        var playbackdata = RemoteStatePlaybackData.FromCluster(cluster);
        var timelinedata = RemoteStateTimelineData.FromCluster(cluster);

        return this with
        {
            Time = timedata,
            Context = contextdata,
            Playback = playbackdata,
            Timeline = timelinedata
        };
    }

    private static DeviceInfo InitializeDeviceInfo(
        string deviceName,
        string deviceId,
        DeviceType deviceType)
    {
        return new DeviceInfo
        {
            CanPlay = true,
            Name = deviceName,
            DeviceId = deviceId,
            DeviceType = deviceType,
            DeviceSoftwareVersion = "Spotify-11.1.0",
            SpircVersion = "3.2.6",
            Capabilities = new Capabilities
            {
                CanBePlayer = true,
                GaiaEqConnectId = true,
                SupportsLogout = true,
                IsObservable = true,
                CommandAcks = true,
                SupportsRename = true,
                SupportsTransferCommand = true,
                SupportsCommandRequest = true,
                SupportsGzipPushes = true,
                NeedsFullPlayerState = true,
                SupportedTypes =
                {
                    new List<string>
                    {
                        { "audio/episode" },
                        { "audio/track" }
                    }
                }
            }
        };
    }

    public SpotifyRemoteState NewCommand(SpotifyRequestMessage spotifyRequestCommand)
    {
        return this with
        {
            LastCommand = Some(new LastCommandData(
                LastCommandSentByDeviceId: spotifyRequestCommand.SentByDeviceId,
                LastMessageId: spotifyRequestCommand.MessageId
            ))
        };
    }

    private PlayerState BuildPlayerState()
    {
        var emptyState = new PlayerState
        {
            PlaybackSpeed = 1.0,
            SessionId = string.Empty,
            PlaybackId = string.Empty,
            Suppressions = new Suppressions(),
            ContextRestrictions = new Restrictions(),
            Options = new ContextPlayerOptions
            {
                RepeatingTrack = false,
                ShufflingContext = false,
                RepeatingContext = false
            },
            Position = 0,
            PositionAsOfTimestamp = 0,
            IsPlaying = false,
            IsSystemInitiated = true
        };

        if (Playback.IsSome)
        {
            var pb = Playback.ValueUnsafe();
            emptyState.Position = pb.PositionAsOfTimestamp;
            emptyState.PositionAsOfTimestamp = pb.PositionAsOfTimestamp;
            emptyState.PlaybackSpeed = 1.0;
            emptyState.SessionId = pb.SessionId.IfNone(string.Empty);
            emptyState.PlaybackId = pb.PlaybackId.IfNone(string.Empty);
            emptyState.IsPlaying = pb.IsPlaying;
            emptyState.IsPaused = pb.IsPaused;
            emptyState.Options = new ContextPlayerOptions
            {
                RepeatingContext = pb.RepeatContext,
                RepeatingTrack = pb.RepeatTrack,
                ShufflingContext = pb.IsShuffling
            };
            emptyState.Duration = pb.Duration;
            pb.Index.IfSome(index => emptyState.Index = new ContextIndex { Track = index });
            pb.Track.IfSome(track => emptyState.Track = track);
            emptyState.PlayOrigin = pb.PlayOrigin;
            emptyState.Restrictions = new Restrictions();
            foreach (var (key, value) in pb.Restrictions)
            {
                foreach (var reason in value)
                {
                    RestrictionsManager.Disallow(emptyState.Restrictions, key, reason);
                }
            }
        }

        if (Context.IsSome)
        {
            var ctx = Context.ValueUnsafe();
            emptyState.ContextRestrictions = new Restrictions();
            foreach (var (key, value) in ctx.Restrictions)
            {
                foreach (var reason in value)
                {
                    RestrictionsManager.Disallow(emptyState.ContextRestrictions, key, reason);
                }
            }

            if (!string.IsNullOrEmpty(ctx.Uri))
            {
                emptyState.ContextUri = ctx.Uri;
                emptyState.ContextUrl = $"context://{ctx.Uri}";
            }

            foreach (var (key, val) in ctx.Metadata)
            {
                emptyState.ContextMetadata[key] = val;
            }
        }

        if (Timeline.IsSome)
        {
            var timeline = Timeline.ValueUnsafe();
            emptyState.NextTracks.AddRange(timeline.NextTracks);
            emptyState.PrevTracks.AddRange(timeline.PrevTracks);
        }

        return emptyState;
    }

    public PutStateRequest BuildPutState(
        PutStateReason reason,
        bool isActive)
    {
        var putStateRequest = new PutStateRequest
        {
            MemberType = MemberType.ConnectState,
            Device = new Device
            {
                DeviceInfo = InitializeDeviceInfo(DeviceName, DeviceId, DeviceType),
                PlayerState = BuildPlayerState()
            },
            PutStateReason = reason,
            IsActive = isActive,
        };

        if (LastCommand.IsSome)
        {
            var lastCommand = LastCommand.ValueUnsafe();
            putStateRequest.LastCommandSentByDeviceId = lastCommand.LastCommandSentByDeviceId;
            putStateRequest.LastCommandMessageId = lastCommand.LastMessageId;
        }

        return putStateRequest;
    }

    public static SpotifyRemoteState CreateNew(
        string deviceId,
        string deviceName,
        DeviceType deviceType,
        string connectionId)
        => new SpotifyRemoteState
        {
            ConnectionId = connectionId,
            DeviceId = deviceId,
            DeviceName = deviceName,
            DeviceType = deviceType
        };
}