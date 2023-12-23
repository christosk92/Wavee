using Eum.Spotify.connectstate;

namespace Wavee.Spotify.Core.Clients.Remote.State;

internal readonly record struct SpotifyLocalState
{
    public DateTimeOffset StartedPlayingAt { get; init; }
    public uint? LastCommandId { get; init; }
    public string? LastCommandSentByDeviceId { get; init; }

    private const uint VOLUME_STEPS = 12;
    public const uint MAX_VOLUME = 65535;

    public PutStateRequest BuildPutState(
        WaveeSpotifyConfig config,
        PutStateReason reason,
        long? playerTime,
        long timestamp)
    {
        var putState = new PutStateRequest
        {
            MemberType = MemberType.ConnectState,
            Device = new Device
            {
                PlayerState = BuildPlayerState(),
                DeviceInfo = new DeviceInfo()
                {
                    CanPlay = true,
                    Volume = (uint)(config.Playback.InitialVolume * MAX_VOLUME),
                    Name = config.Remote.DeviceName,
                    DeviceId = config.Remote.DeviceId,
                    DeviceType = config.Remote.Type,
                    DeviceSoftwareVersion = "Spotify-11.1.0",
                    SpircVersion = "3.2.6",
                    Capabilities = new Capabilities
                    {
                        CanBePlayer = true,
                        GaiaEqConnectId = true,
                        SupportsLogout = true,
                        VolumeSteps = (int)VOLUME_STEPS,
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
            HasBeenPlayingForMs =
                playerTime switch
                {
                    { } t => (ulong)Math.Min(t, timestamp - StartedPlayingAt.ToUnixTimeMilliseconds()),
                    null => (ulong)0
                },
            PutStateReason = reason,
            ClientSideTimestamp = (ulong)timestamp,
            LastCommandMessageId = LastCommandId ?? 0,
            LastCommandSentByDeviceId = LastCommandSentByDeviceId ?? string.Empty
        };

        return putState;
    }

    private PlayerState BuildPlayerState()
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
            PositionAsOfTimestamp = 0,
            Position = 0,
            PlaybackSpeed = 1.0,
            IsPlaying = false
        };
    }

    public static SpotifyLocalState Empty(WaveeSpotifyConfig config)
    {
        return new SpotifyLocalState();
    }
}