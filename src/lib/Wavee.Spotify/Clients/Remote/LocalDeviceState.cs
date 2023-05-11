using System.Diagnostics;
using System.Security.Cryptography;
using Eum.Spotify.connectstate;
using LanguageExt.UnsafeValueAccess;
using Wavee.Spotify.Clients.Playback;

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

    public LocalDeviceState SetContextUri(Option<string> infoContextUri,
        Option<string> contextUrl,
        HashMap<string, string> metadata,
        HashMap<string, Seq<string>> contextRestrictions)
    {
        if (infoContextUri.IsSome)
        {
            var contextUri = infoContextUri.ValueUnsafe();
            State.ContextUri = contextUri;
            State.ContextUrl = contextUrl.IfNone(string.Empty);
            State.ContextMetadata.Clear();
            foreach (var (key, value) in metadata)
            {
                State.ContextMetadata.Add(key, value);
            }

            var restrictions = new RestrictionsManager();
            foreach (var (key, reasons) in contextRestrictions)
            {
                switch (key)
                {
                    case "disallow_toggling_repeat_context_reasons":
                        foreach (var reason in reasons)
                            restrictions.Disallow(RestrictionsManager.Action.REPEAT_CONTEXT, reason);
                        break;
                    case "disallow_toggling_shuffle_reasons":
                        foreach (var reason in reasons)
                            restrictions.Disallow(RestrictionsManager.Action.SHUFFLE, reason);
                        break;
                    default:
                        Debugger.Break();
                        break;
                }
            }

            State.ContextRestrictions = restrictions.ToProto();
            return this with
            {
                State = State
            };
        }

        return this;
    }

    public LocalDeviceState SetDuration(Option<int> infoDuration)
    {
        State.Duration = infoDuration.IfNone(0);
        return this with
        {
            State = State
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

    public LocalDeviceState Paused()
    {
        State.IsPlaying = true;
        State.IsBuffering = false;
        State.IsPaused = true;
        return this with
        {
            State = State
        };
    }

    public LocalDeviceState SetTrack(Option<ProvidedTrack> infoTrack)
    {
        if (infoTrack.IsSome)
        {
            var track = infoTrack.ValueUnsafe();
            State.Track = track;
            track.Metadata["context_uri"] = State.ContextUri;
            return this with
            {
                State = State
            };
        }

        return this;
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

    internal LocalDeviceState SetPosition(long pos)
    {
        State.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        State.PositionAsOfTimestamp = pos;
        State.Position = 0L;
        return this with
        {
            State = State
        };
    }

    public PutStateRequest BuildPutState(PutStateReason reason,
        double volumeFrac,
        Option<TimeSpan> playerTime)
    {
        var putState = new PutStateRequest
        {
            MemberType = MemberType.ConnectState,
            Device = new Device
            {
                DeviceInfo = BuildDeviceInfo(DeviceId, DeviceName, DeviceType,
                    volumeFrac),
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
        double initialVolume)
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

    public LocalDeviceState FromVolume(int commandOptionsMessageId)
    {
        return this with
        {
            LastMessageId = Some((uint)commandOptionsMessageId),
        };
    }

    public LocalDeviceState SetIndex(Option<int> infoIndex)
    {
        if (infoIndex.IsSome)
        {
            State.Index = new ContextIndex
            {
                Track = (uint)infoIndex.ValueUnsafe()
            };
            return this with
            {
                State = State
            };
        }

        return this;
    }
}