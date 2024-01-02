using Eum.Spotify.connectstate;
using Wavee.Core.Enums;
using Wavee.Core.Models;
using Wavee.Spotify.Infrastructure.Playback;

namespace Wavee.Spotify.Core.Mappings;

internal static class PlaybackStateMapping
{
    private const uint VOLUME_STEPS = 12;
    public const uint MAX_VOLUME = 65535;

    public static PlayerState ToPlayerState(this WaveePlaybackState state,
        TimeSpan position,
        string? sessionId,
        SpotifyAudioStream? spotify)
    {
        var playerState = new PlayerState();
        playerState.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        playerState.PositionAsOfTimestamp = (long)position.TotalMilliseconds;
        playerState.Position = 0;

        playerState.Options = new ContextPlayerOptions
        {
            RepeatingContext = state.RepeatMode >= WaveeRepeatStateType.Context,
            RepeatingTrack = state.RepeatMode >= WaveeRepeatStateType.Track,
            ShufflingContext = state.IsShuffling
        };
        if (spotify is not null)
        {
            var spotifyItem = spotify.AsSpotifyItem();
            playerState.Duration = (long)spotify.Duration.TotalMilliseconds;
            playerState.Track = new ProvidedTrack
            {
                Uri = spotifyItem.Uri.ToString(),
                Metadata =
                {
                    { "track_player", "audio" }
                }
            };
            playerState.ContextUri = spotifyItem.Uri.ToString();
            playerState.ContextUrl = $"context://{spotifyItem.Uri.ToString()}";
            playerState.ContextMetadata.Add("player.arch", "2");
            playerState.ContextRestrictions = new Restrictions();
            playerState.PlayOrigin = new PlayOrigin
            {
                FeatureIdentifier = "search",
                ReferrerIdentifier = "search"
            };
        }

        playerState.PlaybackId = state.PlaybackId;
        if (sessionId is not null)
        {
            playerState.SessionId = sessionId;
        }

        playerState.Index = new ContextIndex();

        switch (state.PlaybackState)
        {
            case WaveePlaybackStateType.None:
                playerState.PlaybackSpeed = 0;
                playerState.IsPaused = true;
                playerState.IsPlaying = false;
                playerState.IsBuffering = false;
                break;
            case WaveePlaybackStateType.Buffering:
                playerState.PlaybackSpeed = 0;
                playerState.IsPaused = false;
                playerState.IsPlaying = true;
                playerState.IsBuffering = true;
                break;
            case WaveePlaybackStateType.Playing:
                playerState.PlaybackSpeed = 1;
                playerState.IsPaused = false;
                playerState.IsPlaying = true;
                playerState.IsBuffering = false;
                break;
            case WaveePlaybackStateType.Paused:
                playerState.PlaybackSpeed = 1;
                playerState.IsPaused = true;
                playerState.IsPlaying = true;
                playerState.IsBuffering = false;
                break;
            case WaveePlaybackStateType.Stopped:
                playerState.PlaybackSpeed = 0;
                playerState.IsPaused = true;
                playerState.IsPlaying = false;
                playerState.IsBuffering = false;
                break;
            case WaveePlaybackStateType.Error:
                playerState.PlaybackSpeed = 0;
                playerState.IsPaused = true;
                playerState.IsPlaying = false;
                playerState.IsBuffering = false;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return playerState;
    }

    public static PutStateRequest ToPutState(this PlayerState playerState,
        PutStateReason reason,
        double volume,
        TimeSpan? playerPosition,
        DateTimeOffset? hasBeenPlayingSince,
        DateTimeOffset now,
        string? lastCommandSentBy,
        uint? lastCommandId,
        WaveeSpotifyRemoteConfig remoteConfig)
    {
        var putState = new PutStateRequest
        {
            MemberType = MemberType.ConnectState,
            Device = new Device
            {
                PlayerState = playerState,
                DeviceInfo = new DeviceInfo()
                {
                    CanPlay = true,
                    Volume = (uint)(volume * MAX_VOLUME),
                    Name = remoteConfig.DeviceName,
                    DeviceId = remoteConfig.DeviceId,
                    DeviceType = remoteConfig.Type,
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
                playerPosition switch
                {
                    { } t => (ulong)Math.Min(t.TotalMilliseconds, SubtractSafe(now, hasBeenPlayingSince)),
                    null => (ulong)0
                },
            PutStateReason = reason,
            ClientSideTimestamp = (ulong)now.ToUnixTimeMilliseconds(),
            LastCommandMessageId = lastCommandId ?? 0,
            LastCommandSentByDeviceId = lastCommandSentBy ?? string.Empty
        };
        if (hasBeenPlayingSince is not null)
        {
            putState.IsActive = true;
        }

        return putState;
    }

    private static ulong SubtractSafe(DateTimeOffset now, DateTimeOffset? hasBeenPlayingSince)
    {
        if (hasBeenPlayingSince is null)
        {
            return 0;
        }

        var diff = now - hasBeenPlayingSince.Value;
        //if (diff.TotalMilliseconds < 0)
        if (diff.Ticks < 0)
        {
            return 0;
        }

        return (ulong)diff.TotalMilliseconds;
    }
}