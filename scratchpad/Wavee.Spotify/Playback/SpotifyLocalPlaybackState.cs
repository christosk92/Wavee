using Eum.Spotify.connectstate;

namespace Wavee.Spotify.Playback;

public readonly record struct SpotifyLocalPlaybackState(
    SpotifyRemoteConfig Config,
    string DeviceId,
    PlayerState State,
    bool IsActive,
    uint? LastCommandId,
    string? LastCommandSentBy,
    DateTimeOffset? PlayingSince)
{
    public PutStateRequest BuildPutStateRequest(PutStateReason reason, TimeSpan? playerTime)
    {
        var putState = new PutStateRequest
        {
            MemberType = MemberType.ConnectState,
            Device = new Device
            {
                DeviceInfo = BuildDeviceInfo(DeviceId, Config.DeviceName, Config.DeviceType, 1 * ushort.MaxValue),
                PlayerState = State
            },
            IsActive = IsActive,
            PutStateReason = reason,
        };
        if (playerTime.HasValue)
        {
            putState.HasBeenPlayingForMs = (uint)playerTime.Value.TotalMilliseconds;
        }
        else
        {
            putState.HasBeenPlayingForMs = 0;
        }

        if (PlayingSince.HasValue)
        {
            putState.StartedPlayingAt = (ulong)PlayingSince.Value.ToUnixTimeMilliseconds();
        }
        else
        {
            putState.StartedPlayingAt = 0;
        }

        if (LastCommandId.HasValue)
        {
            putState.LastCommandMessageId = LastCommandId.Value;
        }
        else
        {
            putState.LastCommandMessageId = 0;
        }

        if (!string.IsNullOrEmpty(LastCommandSentBy))
        {
            putState.LastCommandSentByDeviceId = LastCommandSentBy;
        }
        else
        {
            putState.LastCommandSentByDeviceId = string.Empty;
        }

        putState.ClientSideTimestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return putState;
    }

    public static SpotifyLocalPlaybackState Empty(SpotifyRemoteConfig config, string deviceId)
    {
        return new SpotifyLocalPlaybackState(
            Config: config,
            DeviceId: deviceId,
            State: BuildFreshPlayerState(),
            IsActive: false,
            null,
            null,
            null
        );
    }
    private static DeviceInfo BuildDeviceInfo(string deviceId, string deviceName, DeviceType deviceType,
        float initialVolume)
    {
        return new DeviceInfo
        {
            CanPlay = true,
            Volume = (uint)(initialVolume / ushort.MaxValue),
            Name = deviceName,
            DeviceId = deviceId,
            DeviceType = deviceType,
            DeviceSoftwareVersion = "1.0.0",
            SpircVersion = "3.2.6",
            Capabilities = new Capabilities
            {
                CanBePlayer = true,
                GaiaEqConnectId = true,
                SupportsLogout = true,
                IsObservable = true,
                CommandAcks = true,
                SupportsRename = false,
                SupportsPlaylistV2 = true,
                IsControllable = true,
                SupportsTransferCommand = true,
                SupportsCommandRequest = true,
                VolumeSteps = (int)64,
                SupportsGzipPushes = true,
                NeedsFullPlayerState = false,
                SupportedTypes = { "audio/episode", "audio/track" }
            }
        };
    }

    private static PlayerState BuildFreshPlayerState()
    {
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
            Track = new ProvidedTrack(),
            PositionAsOfTimestamp = 0,
            Position = 0,
            PlaybackSpeed = 1.0,
            IsPlaying = false
        };
    }
}