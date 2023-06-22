using Eum.Spotify.connectstate;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Player;
using Wavee.Player.State;

namespace Wavee.Playback;

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

    public SpotifyLocalPlaybackState FromPlayer(Option<WaveePlayerState> waveePlayerState, bool isActive,
        bool activeChanged,
        Option<string> lastCommandSentBy,
        Option<uint> lastCommandId)
    {
        var state = BuildFreshPlayerState();
        if (waveePlayerState.IsNone)
            return this with
            {
                State = state,
                IsActive = isActive,
                PlayingSince = Option<DateTimeOffset>.None
            };
        var stateValue = waveePlayerState.ValueUnsafe();
        state.Position = 0;
        state.SessionId = stateValue.SessionId.IfNone(string.Empty);
        state.PlaybackId = stateValue.PlaybackId.IfNone(string.Empty);
        state.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        state.PositionAsOfTimestamp = (long)stateValue.Position.IfNone(TimeSpan.Zero).TotalMilliseconds;
        state.Options = new ContextPlayerOptions
        {
            RepeatingContext = stateValue.RepeatState >= RepeatState.Context,
            RepeatingTrack = stateValue.RepeatState >= RepeatState.Track,
            ShufflingContext = stateValue.Shuffling
        };
        if (stateValue.Context.IsSome)
        {
            var ctx = stateValue.Context.ValueUnsafe();
            var id = ctx.Id;
            state.ContextUri = id;
            state.ContextUrl = $"context://{id}";
            state.ContextMetadata.Clear();
            state.ContextRestrictions = new Restrictions();
        }

        switch (stateValue.State)
        {
            case WaveePlaybackStateType.Loading:
                state.IsBuffering = true;
                state.IsPlaying = true;
                state.IsPaused = false;
                break;
            case WaveePlaybackStateType.Playing:
                state.IsBuffering = false;
                state.IsPlaying = true;
                state.IsPaused = false;
                break;
            case WaveePlaybackStateType.Paused:
                state.IsBuffering = false;
                state.IsPlaying = true;
                state.IsPaused = true;
                break;
            case WaveePlaybackStateType.PermanentEndOfContext:
                state.IsBuffering = false;
                state.IsPlaying = false;
                state.IsPaused = false;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (stateValue.TrackId.IsSome)
        {
            state.Track = new ProvidedTrack
            {
                Uri = stateValue.TrackId.ValueUnsafe()
            };
        }

        if (stateValue.Track.IsSome)
        {
            var trackVal = stateValue.Track.ValueUnsafe();
            state.Track = new ProvidedTrack
            {
                Uri = trackVal.Id,
                Uid = trackVal.Metadata.Find("uid").IfNone(string.Empty).ToString(),
            };
            state.Duration = (uint)trackVal.Duration.TotalMilliseconds;
            state.Track.Metadata.Clear();
            foreach (var (key, value) in trackVal.Metadata)
            {
                state.Track.Metadata.Add(key, value.ToString());
            }
        }

        return this with
        {
            State = state,
            IsActive = isActive,
            PlayingSince = (isActive && activeChanged)
                ? Option<DateTimeOffset>.Some(DateTimeOffset.UtcNow)
                : PlayingSince,
            LastCommandId = lastCommandId,
            LastCommandSentBy = lastCommandSentBy
        };
    }
    // public SpotifyLocalPlaybackState FromPlayer(WaveePlayerState waveePlayerState, bool isActive, bool activeChanged)
    // {
    //     var state = this.State;
    //
    //     state.Position = 0;
    //     state.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    //     state.PositionAsOfTimestamp = (long)WaveePlayer.Instance.Position.IfNone(TimeSpan.Zero).TotalMilliseconds;
    //     state.Options = new ContextPlayerOptions
    //     {
    //         RepeatingContext = waveePlayerState.RepeatState is RepeatState.Context,
    //         RepeatingTrack = waveePlayerState.RepeatState is RepeatState.Track,
    //         ShufflingContext = waveePlayerState.IsShuffling
    //     };
    //     if (waveePlayerState.Context.IsSome)
    //     {
    //         var ctx = waveePlayerState.Context.ValueUnsafe();
    //         var id = ctx.Id;
    //         State.ContextUri = id;
    //         State.ContextUrl = $"context://{id}";
    //         State.ContextMetadata.Clear();
    //         State.ContextRestrictions = new Restrictions();
    //     }
    //
    //     if (waveePlayerState.TrackId.IsSome)
    //     {
    //         State.IsBuffering = false;
    //         State.IsPlaying = true;
    //         State.IsPaused = waveePlayerState.IsPaused;
    //         State.Track = new ProvidedTrack
    //         {
    //             Uri = waveePlayerState.TrackId.ValueUnsafe().ToString()
    //         };
    //         State.Duration = (long)waveePlayerState.TrackDetails.ValueUnsafe().Duration.TotalMilliseconds;
    //
    //         if (waveePlayerState.TrackUid.IsSome)
    //         {
    //             State.Track.Uid = waveePlayerState.TrackUid.ValueUnsafe();
    //         }
    //
    //
    //         State.Track.Metadata.Clear();
    //         foreach (var (key, value) in waveePlayerState.TrackDetails.ValueUnsafe().Metadata)
    //         {
    //             if (value is string v)
    //                 State.Track.Metadata[key] = v;
    //         }
    //     }
    //
    //
    //     return this with
    //     {
    //         IsActive = isActive,
    //         PlayingSince = (isActive && activeChanged) ? DateTimeOffset.UtcNow : PlayingSince,
    //         State = state
    //     };
    // }

    public Option<DateTimeOffset> PlayingSince { get; init; }

    public SpotifyLocalPlaybackState SetPosition(TimeSpan pos)
    {
        var st = State;
        st.Position = 0l;
        st.PositionAsOfTimestamp = (long)pos.TotalMilliseconds;
        st.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return this with
        {
            State = st
        };
    }
}