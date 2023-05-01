using Eum.Spotify.connectstate;
using Wavee.Spotify.Remote.Infrastructure.Sys;
using Wavee.Spotify.Remote.Infrastructure.Traits;

namespace Wavee.Spotify.Remote.State;

internal readonly record struct SpotifyRemoteState<RT>(
    ulong StartedPlayingAt,
    Option<uint> LastCommandId,
    Option<string> LastCommandSentByDeviceId)
    where RT : struct, HasTime<RT>
{
    private readonly string _deviceId;
    private readonly SpotifyPlaybackConfig _config;

    private readonly RT _rt;

    public SpotifyRemoteState(SpotifyPlaybackConfig config, string deviceId, RT rt) :
        this(0, Option<uint>.None, Option<string>.None)
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
        var putState = new PutStateRequest
        {
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
        return new PlayerState
        {
            SessionId = string.Empty,
            PlaybackId = string.Empty,
            Suppressions = new Suppressions(),
            ContextRestrictions = new Restrictions(),
            Options = new ContextPlayerOptions
            {
                RepeatingContext = false,
                RepeatingTrack = false,
                ShufflingContext = false
            },
            PositionAsOfTimestamp = 0, Position = 0,
            PlaybackSpeed = 1.0,
            IsPlaying = false
        };
    }
}