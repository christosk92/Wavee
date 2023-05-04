using Eum.Spotify.connectstate;
using LanguageExt.UnsafeValueAccess;
using Wavee.Spotify.Models;
using Wavee.Spotify.Playback.Context;
using Wavee.Spotify.Remote.Infrastructure.Sys;
using Wavee.Spotify.Remote.Infrastructure.Traits;

namespace Wavee.Spotify.Remote.State;

internal readonly record struct SpotifyRemoteState<RT>(
    ulong StartedPlayingAt,
    Option<int> PositionAsOfTimestamp,
    Option<ulong> Timestamp,
    Option<long> Position,
    Option<uint> LastCommandId,
    Option<string> LastCommandSentByDeviceId,
    bool ShufflingContext,
    bool RepeatingContext,
    bool RepeatingTrack,
    bool IsPaused,
    bool IsPlaying,
    Option<global::Eum.Spotify.connectstate.PlayOrigin> PlayOrigin,
    Option<string> ContextUrl,
    Option<string> ContextUri,
    Option<string> SessionId,
    Option<string> PlaybackId,
    Option<SpotifyId> TrackId,
    Option<string> TrackUid)
    where RT :
    struct, HasTime<RT>
{
    private readonly string _deviceId;
    private readonly SpotifyPlaybackConfig _config;
    private readonly RT _rt;

    public SpotifyRemoteState(
        SpotifyPlaybackConfig config,
        string deviceId,
        RT rt) :
        this(0,
            Option<int>.None,
            Option<ulong>.None,
            Option<long>.None,
            Option<uint>.None,
            Option<string>.None, false, false, false, false, false,
            Option<global::Eum.Spotify.connectstate.PlayOrigin>.None,
            Option<string>.None,
            Option<string>.None,
            Option<string>.None,
            Option<string>.None,
            Option<SpotifyId>.None,
            Option<string>.None)
    {
        _config = config;
        _deviceId = deviceId;
        _rt = rt;
    }

    public PutStateRequest BuildPutStateRequest(PutStateReason because, Option<ulong> playerTime)
    {
        var timestamp = Time<RT>.timestamp.Run(_rt)
            .Match(
                Succ: t => t,
                Fail: e => throw new Exception("Failed to get timestamp"));

        var startedPlayingAt = StartedPlayingAt;

        var isActive = playerTime.Match(
            Some: t => t > startedPlayingAt,
            None: () => false);

        var putState = new PutStateRequest
        {
            IsActive = isActive,
            StartedPlayingAt = startedPlayingAt,
            MemberType = MemberType.ConnectState,
            Device = new Device
            {
                PlayerState = BuildPlayerState(),
                DeviceInfo = new DeviceInfo()
                {
                    CanPlay = true,
                    Volume = _config.InitialVolume,
                    Name = _config.DeviceName,
                    DeviceId = _deviceId,
                    DeviceType = _config.DeviceType,
                    DeviceSoftwareVersion = "Spotify-11.1.0",
                    SpircVersion = "3.2.6",
                    Capabilities = new Capabilities
                    {
                        CanBePlayer = true,
                        GaiaEqConnectId = true,
                        SupportsLogout = true,
                        VolumeSteps = _config.VolumeSteps,
                        IsObservable = true,
                        CommandAcks = true,
                        SupportsRename = false,
                        SupportsPlaylistV2 = true,
                        IsControllable = true,
                        SupportsCommandRequest = true,
                        SupportsTransferCommand = true,
                        SupportsGzipPushes = true,
                        NeedsFullPlayerState = false,
                        SupportedTypes = { "audio/episode", "audio/track" }
                    }
                }
            },
            HasBeenPlayingForMs = playerTime.Match(
                Some: t => (ulong)Math.Min(t, timestamp - startedPlayingAt),
                None: () => (ulong)0
            ),
            PutStateReason = because,
            ClientSideTimestamp = timestamp,
            LastCommandMessageId = LastCommandId.IfNone(0),
            LastCommandSentByDeviceId = LastCommandSentByDeviceId.IfNone(string.Empty)
        };

        return putState;
    }

    private PlayerState BuildPlayerState()
    {
        //TODO
        var state = new PlayerState
        {
            SessionId = SessionId.IfNone(string.Empty),
            PlaybackId = PlaybackId.IfNone(string.Empty),
            Suppressions = new Suppressions(), //TODO
            ContextRestrictions = new Restrictions(), //TODO
            Options = new ContextPlayerOptions
            {
                RepeatingContext = RepeatingContext,
                RepeatingTrack = RepeatingTrack,
                ShufflingContext = ShufflingContext
            },
            PositionAsOfTimestamp = PositionAsOfTimestamp.IfNone(0),
            Timestamp = (long)Timestamp.IfNone(0),
            Position = Position.IfNone(0),
            PlaybackSpeed = 1.0,
            IsPlaying = IsPlaying,
            IsPaused = IsPaused,
            ContextUri = ContextUri.IfNone(string.Empty),
            ContextUrl = ContextUrl.IfNone(string.Empty),
        };
        if (PlayOrigin.IsSome)
        {
            state.PlayOrigin = PlayOrigin.ValueUnsafe();
        }

        if (TrackId.IsSome)
        {
            var trackId = TrackId.ValueUnsafe();
            state.Track = new ProvidedTrack
            {
                Uri = trackId.Uri,
            };
        }

        if (TrackUid.IsSome)
        {
            var trackUid = TrackUid.ValueUnsafe();
            state.Track ??= new ProvidedTrack();
            state.Track.Uid = trackUid;
        }

        return state;
    }

    public Eff<RT, Option<ISpotifyContext>> BuildContext()
    {
        var contextUri = ContextUri;
        var trackId = TrackId.Map(x => x.Uri);
        var trackUid = TrackUid;
        return Eff<RT, Option<ISpotifyContext>>(rt =>
        {
            return contextUri.Match(
                Some: uri => new GenericSpotifyContext<RT>(
                    uri,
                    trackId,
                    trackUid,
                    rt),
                None: () => Option<ISpotifyContext>.None);
        });
    }

    public int GetPosition()
    {
        var time = Time<RT>.timestamp.Run(_rt)
            .Match(
                Succ: t => t,
                Fail: e => throw new Exception("Failed to get timestamp"));

        var positionAsOfTimestamp = PositionAsOfTimestamp.IfNone(0);
        var diff = (int)(time - Timestamp.IfNone(0));
        return positionAsOfTimestamp + diff;
    }
}