using System.Security.Cryptography;
using Eum.Spotify.connectstate;
using LanguageExt.UnsafeValueAccess;

namespace Wavee.Spotify.Clients.Remote;

public readonly record struct LocalDeviceState(
    string ConnectionId,
    string DeviceId,
    string DeviceName,
    DeviceType DeviceType,
    Option<uint> LastMessageId,
    Option<string> LastCommandSentByDeviceId,
    Option<ulong> StartedPlayingAt,
    bool IsActive,
    PlayerState State)
{
    public static LocalDeviceState New(string connectionId, string deviceId,
        string deviceName, DeviceType deviceType)
    {
        return new LocalDeviceState(connectionId, deviceId, deviceName, deviceType,
            Option<uint>.None,
            Option<string>.None,
            Option<ulong>.None,
            false,
            BuildFreshPlayerState());
    }

    public LocalDeviceState SetActive(bool isActive)
    {
        bool wasActive = IsActive;
        return this with
        {
            IsActive = isActive,
            StartedPlayingAt = isActive
                ? (wasActive ? this.StartedPlayingAt : Some((ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()))
                : None
        };
    }

    public LocalDeviceState Buffering(Option<string> trackUri)
    {
        State.IsPlaying = false;
        State.IsBuffering = true;
        State.IsPaused = false;
        return this with
        {
            State = State
        };
    }

    public LocalDeviceState Playing()
    {
        var wasPaused = State.IsPaused;
        State.IsPlaying = true;
        State.IsBuffering = false;
        State.IsPaused = false;

        if (wasPaused)
        {
            return SetPosition(State.PositionAsOfTimestamp)
                with
                {
                    State = State
                };
        }

        return this with
        {
            State = State
        };
    }

    private LocalDeviceState SetPosition(long pos)
    {
        State.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        State.PositionAsOfTimestamp = pos;
        State.Position = 0L;
        return this with
        {
            State = State
        };
    }

    public PutStateRequest BuildPutState(PutStateReason reason, Option<TimeSpan> playerTime)
    {
        var putState = new PutStateRequest
        {
            MemberType = MemberType.ConnectState,
            Device = new Device
            {
                DeviceInfo = BuildDeviceInfo(DeviceId, DeviceName, DeviceType, 1 * ushort.MaxValue),
                PlayerState = State
            },
            IsActive = IsActive,
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

        if (StartedPlayingAt.IsSome)
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
            PositionAsOfTimestamp = 0, Position = 0,
            PlaybackSpeed = 1.0,
            IsPlaying = false
        };
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


    public LocalDeviceState FromClusterUpdate(PlayerState clusterPlayerState)
    {
        static string GenerateSessionId()
        {
            var bytes = new byte[16];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }

        clusterPlayerState ??= BuildFreshPlayerState();
        //strip some fields
        clusterPlayerState.PlaybackQuality = new PlaybackQuality();
        clusterPlayerState.SessionId = GenerateSessionId();
        return this with
        {
            State = clusterPlayerState
        };
    }
}