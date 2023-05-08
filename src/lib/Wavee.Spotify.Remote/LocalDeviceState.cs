using Eum.Spotify.connectstate;
using LanguageExt.UnsafeValueAccess;

namespace Wavee.Spotify.Remote;

internal readonly record struct LocalDeviceState(PlayerState State,
    DeviceInfo DeviceInfo,
    Option<uint> LastMessageId,
    Option<string> LastCommandSentByDeviceId,
    Option<ulong> StartedPlayingAt)
{
    public static LocalDeviceState New(string deviceId, string deviceName, DeviceType deviceType, float initialVolume)
    {
        return new LocalDeviceState(
            State: BuildFreshState(),
            DeviceInfo: BuildDeviceInfo(deviceId, deviceName, deviceType, initialVolume),
            Option<uint>.None,
            Option<string>.None,
            Option<ulong>.None
        );
    }

    private static DeviceInfo BuildDeviceInfo(string deviceId, string deviceName, DeviceType deviceType,
        float initialVolume)
    {
        return new DeviceInfo
        {
            CanPlay = true,
            Volume = (uint)(initialVolume * ushort.MaxValue),
            Name = deviceName,
            DeviceId = deviceId,
            DeviceType = deviceType,
            DeviceSoftwareVersion = "1.0.0",
            ClientId = SpotifyConstants.KEYMASTER_CLIENT_ID,
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

    private static PlayerState BuildFreshState()
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
            PositionAsOfTimestamp = 0, Position = 0,
            PlaybackSpeed = 1.0,
            IsPlaying = false
        };
    }

    public PutStateRequest BuildPutState(PutStateReason reason, Option<TimeSpan> playerTime, bool isActive = false)
    {
        var putState = new PutStateRequest
        {
            MemberType = MemberType.ConnectState,
            Device = new Device
            {
                DeviceInfo = DeviceInfo,
                PlayerState = State
            },
            IsActive = isActive,
            PutStateReason = reason,
        };
        if (playerTime.IsSome)
        {
            putState.HasBeenPlayingForMs = (uint)playerTime.ValueUnsafe().TotalMilliseconds;
        }
        else
        {
            putState.HasBeenPlayingForMs = 0;
        }

        if (LastMessageId.IsSome)
        {
            putState.LastCommandMessageId = (uint)LastMessageId.ValueUnsafe();
        }

        if (LastCommandSentByDeviceId.IsSome)
        {
            putState.LastCommandSentByDeviceId = LastCommandSentByDeviceId.ValueUnsafe();
        }
        else
        {
            putState.LastCommandSentByDeviceId = string.Empty;
        }
        if(StartedPlayingAt.IsSome)
        {
            putState.StartedPlayingAt = StartedPlayingAt.ValueUnsafe();
        }
        else
        {
            putState.StartedPlayingAt = 0;
        }

        putState.ClientSideTimestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        return putState;
    }
}