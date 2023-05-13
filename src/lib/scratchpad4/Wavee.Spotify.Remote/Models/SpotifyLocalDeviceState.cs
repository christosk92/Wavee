using Eum.Spotify.connectstate;
using LanguageExt;

namespace Wavee.Spotify.Remote.Models;

public readonly record struct SpotifyLocalDeviceState(
    string DeviceId, string DeviceName, DeviceType DeviceType, bool IsActive)
{
    public PutStateRequest BuildPutState(PutStateReason reason,
        Option<TimeSpan> playerTime)
    {
        var putState = new PutStateRequest
        {
            MemberType = MemberType.ConnectState,
            Device = new Device
            {
                DeviceInfo = BuildDeviceInfo(DeviceId, DeviceName, DeviceType, 1 * ushort.MaxValue),
                PlayerState = BuildState(this)
            },
            IsActive = IsActive,
            PutStateReason = reason,
        };

        return putState;
    }


    public static SpotifyLocalDeviceState New(string deviceId, string deviceName, DeviceType deviceType)
    {
        return new SpotifyLocalDeviceState(deviceId, deviceName, deviceType, false);
    }

    private static PlayerState BuildState(SpotifyLocalDeviceState spotifyLocalDeviceState)
    {
        return new PlayerState();
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
}