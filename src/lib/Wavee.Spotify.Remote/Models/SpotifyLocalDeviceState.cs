using System.Security.Cryptography;
using Eum.Spotify.connectstate;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Spotify.Metadata;
using Wavee.Core.Enums;
using Wavee.Core.Id;
using Wavee.Player;
using Wavee.Player.States;
using Wavee.Spotify.Playback.Infrastructure.Sys;

namespace Wavee.Spotify.Remote.Models;

public readonly record struct SpotifyLocalDeviceState(
    string DeviceId,
    string DeviceName,
    DeviceType DeviceType,
    bool IsActive,
    Option<DateTimeOffset> PlayingSince)
{
    public PlayerState State { get; init; } = BuildFreshPlayerState();

    public PutStateRequest BuildPutState(PutStateReason reason,
        Option<TimeSpan> playerTime)
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

        return putState;
    }


    public static SpotifyLocalDeviceState New(string deviceId, string deviceName, DeviceType deviceType)
    {
        return new SpotifyLocalDeviceState(deviceId, deviceName, deviceType, false, Option<DateTimeOffset>.None);
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

    public SpotifyLocalDeviceState SetShuffling(bool isShuffling)
    {
        State.Options.ShufflingContext = isShuffling;
        return this with
        {
            State = State
        };
    }

    public SpotifyLocalDeviceState SetRepeatState(RepeatStateType repeatStateType)
    {
        State.Options.RepeatingContext = repeatStateType is RepeatStateType.RepeatContext;
        State.Options.RepeatingTrack = repeatStateType is RepeatStateType.RepeatTrack;
        return this with
        {
            State = State
        };
    }

    public SpotifyLocalDeviceState SetLoading(WaveeLoadingState loading)
    {
        State.IsBuffering = true;
        State.IsPlaying = false;
        State.IsPaused = loading.StartPaused;
        State.Track = new ProvidedTrack();
        if (loading.TrackId.IsSome)
        {
            var value = loading.TrackId.ValueUnsafe();
            var typeStr = value.Type switch
            {
                AudioItemType.Track => "track",
                AudioItemType.PodcastEpisode => "episode",
            };
            State.Track.Uri = $"spotify:{typeStr}:{value.Id}";
        }

        if (WaveePlayer.State.Context.IsSome)
        {
            var id = WaveePlayer.State.Context.ValueUnsafe().Id;
            State.ContextUri = id;
            State.ContextUrl = $"context://{id}";
            State.ContextMetadata.Clear();
            State.ContextRestrictions = new Restrictions();
        }

        State.SessionId = GenerateSessionId();

        return (this with
        {
            State = State
        }).SetPosition(loading.StartFrom);
    }


    public SpotifyLocalDeviceState SetPlaying(WaveePlayingState playing)
    {
        var uid = ((ISpotifyStream)playing.Stream).Uid;
        if (uid.IsSome)
        {
            State.Track.Uid = uid.ValueUnsafe();
        }

        if (playing.IndexInContext.IsSome)
        {
            State.Index = new ContextIndex
            {
                Track = (uint)playing.IndexInContext.ValueUnsafe()
            };
        }

        State.IsPlaying = true;
        State.IsBuffering = false;
        State.IsPaused = false;
        State.Duration = (long)playing.Track.Duration.TotalMilliseconds;
        return (this with
        {
            State = State
        }).SetPosition(playing.Position);
    }

    public SpotifyLocalDeviceState SetPaused(WaveePausedState paused)
    {
        throw new NotImplementedException();
    }

    private SpotifyLocalDeviceState SetPosition(TimeSpan position)
    {
        State.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        State.PositionAsOfTimestamp = (long)position.TotalMilliseconds;
        State.Position = 0L;
        return this with
        {
            State = State
        };
    }

    internal static string GenerateSessionId()
    {
        Span<byte> bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace("-", "")
            .Replace('+', '_') // replace URL unsafe characters with safe ones
            .Replace('/', '_') // replace URL unsafe characters with safe ones
            .Replace("=", ""); // no padding
    }
}