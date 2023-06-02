using Eum.Spotify.connectstate;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Player;

namespace Wavee.Spotify.Infrastructure.Playback.Contracts;

public readonly record struct SpotifyLocalPlaybackState(
    SpotifyRemoteConfig Config,
    string DeviceId,
    PlayerState State,
    bool IsActive,
    Option<uint> LastCommandId,
    Option<string> LastCommandSentBy)
{
    public static SpotifyLocalPlaybackState Empty(SpotifyRemoteConfig config, string deviceId)
    {
        return new SpotifyLocalPlaybackState(
            Config: config,
            DeviceId: deviceId,
            State: BuildFreshPlayerState(),
            IsActive: false,
            Option<uint>.None,
            Option<string>.None
        );
    }

    public PutStateRequest BuildPutStateRequest(PutStateReason reason, Option<TimeSpan> playerTime)
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
        if (playerTime.IsSome)
        {
            putState.HasBeenPlayingForMs = (uint)playerTime.ValueUnsafe().TotalMilliseconds;
        }
        else
        {
            putState.HasBeenPlayingForMs = 0;
        }

        if (PlayingSince.IsSome)
        {
            putState.StartedPlayingAt = (ulong)PlayingSince.ValueUnsafe().ToUnixTimeMilliseconds();
        }
        else
        {
            putState.StartedPlayingAt = 0;
        }
        if (LastCommandId.IsSome)
        {
            putState.LastCommandMessageId = LastCommandId.ValueUnsafe();
        }
        else
        {
            putState.LastCommandMessageId = 0;
        }

        if (LastCommandSentBy.IsSome)
        {
            putState.LastCommandSentByDeviceId = LastCommandSentBy.ValueUnsafe();
        }
        else
        {
            putState.LastCommandSentByDeviceId = string.Empty;
        }
        putState.ClientSideTimestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return putState;
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

    public SpotifyLocalPlaybackState FromPlayer(WaveePlayerState waveePlayerState, bool isActive, bool activeChanged)
    {
        var state = this.State;

        state.Options = new ContextPlayerOptions
        {
            RepeatingContext = waveePlayerState.RepeatState is RepeatState.Context,
            RepeatingTrack = waveePlayerState.RepeatState is RepeatState.Track,
            ShufflingContext = waveePlayerState.IsShuffling
        };  
        if (waveePlayerState.Context.IsSome)
        {
            var ctx = waveePlayerState.Context.ValueUnsafe();
            var id = ctx.Id;
            State.ContextUri = id;
            State.ContextUrl = $"context://{id}";
            State.ContextMetadata.Clear();
            State.ContextRestrictions = new Restrictions();
        }

        if (waveePlayerState.TrackId.IsSome)
        {
            State.IsBuffering = false;
            State.IsPlaying = true;
            State.IsPaused = waveePlayerState.IsPaused;
            State.Track = new ProvidedTrack
            {
                Uri = waveePlayerState.TrackId.ValueUnsafe().ToString()
            };
            State.Duration = (long)waveePlayerState.TrackDetails.ValueUnsafe().Duration.TotalMilliseconds;

            if (waveePlayerState.TrackUid.IsSome)
            {
                State.Track.Uid = waveePlayerState.TrackUid.ValueUnsafe();
            }
            
            
            State.Track.Metadata.Clear();
            foreach (var (key, value) in waveePlayerState.TrackDetails.ValueUnsafe().Metadata)
            {
                State.Track.Metadata[key] = value;
            }
        }
        
        
        return this with
        {
            IsActive = isActive,
            PlayingSince = (isActive && activeChanged) ? DateTimeOffset.UtcNow : PlayingSince,
            State = state
        };
    }

    public Option<DateTimeOffset> PlayingSince { get; init; }
}