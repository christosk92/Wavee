using Eum.Spotify.connectstate;
using Eum.Spotify.context;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Spotify.Remote.Infrastructure.Sys;
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

internal readonly record struct SpotifyRemoteState(
    Option<RemoteStateTimeData> Time,
    Option<RemoteStateContextData> Context,
    Option<RemoteStatePlaybackData> Playback,
    Option<RemoteStateTimelineData> Timeline)
{
    public SpotifyRemoteState() : this(None, None, None, None)
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

        var state = Playback.Match(
            None: () => emptyState,
            Some: p =>
            {
                return
                    Some(emptyState)
                        .Bind(setTime =>
                        {
                            setTime.Timestamp = p.Timestamp;
                            setTime.PositionAsOfTimestamp = p.PositionAsOfTimestamp;
                            return Some(setTime);
                        })
                        .Bind(setIndex =>
                        {
                            return p.Index.Map(f =>
                            {
                                setIndex.Index = new ContextIndex
                                {
                                    Track = f
                                };
                                return setIndex;
                            });
                        })
                        .Bind(setTrack =>
                        {
                            return p.Track.Map(f =>
                            {
                                setTrack.Track = f;
                                return setTrack;
                            });
                        })
                        .Bind(setRestrictions =>
                        {
                            RestrictionsManager.AllowEverything(setRestrictions.Restrictions);
                            foreach (var restriction in p.Restrictions)
                            {
                                foreach (var reason in restriction.Value)
                                {
                                    RestrictionsManager.Disallow(setRestrictions.Restrictions, restriction.Key, reason);
                                }
                            }

                            return Some(setRestrictions);
                        })
                        .Bind(setPlaybackId =>
                        {
                            return p.PlaybackId.Map(f =>
                            {
                                setPlaybackId.PlaybackId = f;
                                return setPlaybackId;
                            });
                        })
                        .Bind(setSessionId =>
                        {
                            return p.SessionId.Map(f =>
                            {
                                setSessionId.SessionId = f;
                                return setSessionId;
                            });
                        })
                        .Bind(setDuration =>
                        {
                            setDuration.Duration = p.Duration;
                            return Some(setDuration);
                        })
                        .Bind(setOptions =>
                        {
                            setOptions.IsPaused = p.IsPaused;
                            setOptions.IsPlaying = p.IsPlaying;
                            setOptions.Options = new ContextPlayerOptions
                            {
                                RepeatingContext = p.RepeatContext,
                                RepeatingTrack = p.RepeatTrack,
                                ShufflingContext = p.IsShuffling
                            };
                            setOptions.PlayOrigin = p.PlayOrigin;
                            return Some(setOptions);
                        });
            }
        );

        return state.Match(
            Some: s => s,
            None: () => emptyState
        );
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