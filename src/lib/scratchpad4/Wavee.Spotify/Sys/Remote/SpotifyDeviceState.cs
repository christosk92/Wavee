using Eum.Spotify.connectstate;
using Wavee.Player;
using PlayOrigin = Eum.Spotify.context.PlayOrigin;

namespace Wavee.Spotify.Sys.Remote;

internal readonly record struct SpotifyDeviceState(
    SpotifyRemoteConfig Config,
    string DeviceId,
    Option<int> LastCommandId,
    Option<string> LastCommandSentByDeviceId,
    Option<Eum.Spotify.connectstate.PlayOrigin> PlayOrigin)
{
    public SpotifyDeviceState WithPlayOrigin(PlayOrigin currentSessionPlayOrigin)
    {
        var newPlayOrigin = new Eum.Spotify.connectstate.PlayOrigin();

        if (currentSessionPlayOrigin.HasDeviceIdentifier)
            newPlayOrigin.DeviceIdentifier = currentSessionPlayOrigin.DeviceIdentifier;
        if (currentSessionPlayOrigin.HasFeatureIdentifier)
            newPlayOrigin.FeatureIdentifier = currentSessionPlayOrigin.FeatureIdentifier;
        if (currentSessionPlayOrigin.HasFeatureVersion)
            newPlayOrigin.FeatureVersion = currentSessionPlayOrigin.FeatureVersion;
        if (currentSessionPlayOrigin.HasViewUri)
            newPlayOrigin.ViewUri = currentSessionPlayOrigin.ViewUri;
        if (currentSessionPlayOrigin.HasReferrerIdentifier)
            newPlayOrigin.ReferrerIdentifier = currentSessionPlayOrigin.ReferrerIdentifier;
        foreach (var classe in currentSessionPlayOrigin.FeatureClasses)
            newPlayOrigin.FeatureClasses.Add(classe);

        return this with { PlayOrigin = newPlayOrigin };
    }
    
    public SpotifyDeviceState WithTrack(PlayingItem item)
    {
        throw new NotImplementedException();
    }

    public PutStateRequest BuildPutStateRequest(PutStateReason reason, Option<TimeSpan> playerTime)
    {
        return new PutStateRequest
        {
            PutStateReason = reason,
            MemberType = MemberType.ConnectState,
            Device = new Device
            {
                PlayerState = BuildPlayerState(this),
                DeviceInfo = new DeviceInfo
                {
                    CanPlay = true,
                    Volume = (uint)(Config.InitialVolume * ushort.MaxValue),
                    Name = Config.DeviceName,
                    DeviceId = DeviceId,
                    DeviceType = Config.DeviceType,
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
                }
            }
        };
    }

    private static PlayerState BuildPlayerState(SpotifyDeviceState deviceState)
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
}