using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using Eum.Spotify.connectstate;
using Eum.Spotify.context;
using Eum.Spotify.transfer;
using Microsoft.Extensions.Logging;
using NeoSmart.AsyncLock;
using Spotify.Metadata;
using Wavee.Config;
using Wavee.Enums;
using Wavee.Interfaces;
using Wavee.Models.Common;
using Wavee.Models.Remote.Commands;
using Wavee.Models.Remote.Commands.Play;
using Wavee.Playback.Contexts;
using Wavee.Playback.Player;
using Wavee.Playback.Streaming;
using Wavee.Services.Playback.Remote;
using PlayOrigin = Eum.Spotify.connectstate.PlayOrigin;
using Restrictions = Eum.Spotify.connectstate.Restrictions;

namespace Wavee.Services.Playback;

internal sealed class SpotifyPlaybackClient : ISpotifyPlaybackClient
{
    private readonly ISpotifyWebsocketState _websocketState;

    private readonly BehaviorSubject<ISpotifyPlaybackState> _playbackStateSubj = new(NonePlaybackState.Instance);
    private readonly ILogger<SpotifyPlaybackClient> _logger;
    private readonly ISpotifyApiClient _apiClient;
    private readonly IAudioKeyManager _audioKeyManager;
    private readonly SpotifyConfig _config;
    private readonly IWaveePlayer _player;
    private readonly ISpotifyPlaylistClient _playlistClient;

    private Device? _localPlayerState;
    private string? _lastCommandSentByDeviceId;
    private int? _lastCommandMessageId;

    private SpotifyLocalPlaybackState? _lastLocalPlaybackState;
    private SpotifyRemotePlaybackState? _lastRemotePlaybackState;
    private ISpotifyPlaybackState? _lastEmittedPlaybackState;
    private readonly AsyncLock _stateLock = new();
    private readonly ILoggerFactory _loggerFactory;
    private PlayOrigin? _playOrigin;
    private readonly ISpotifySessionHolder _sessionHolder;

    public SpotifyPlaybackClient(SpotifyConfig config,
        IWaveePlayer player,
        ISpotifyWebsocketState websocketState,
        ISpotifyApiClient apiClient,
        IAudioKeyManager audioKeyManager,
        ISpotifySessionHolder sessionHolder,
        ILoggerFactory loggerFactory,
        ISpotifyPlaylistClient playlistClient, ITimeProvider timeProvider)
    {
        _sessionHolder = sessionHolder;
        _websocketState = websocketState;
        _apiClient = apiClient;
        _audioKeyManager = audioKeyManager;
        _logger = loggerFactory.CreateLogger<SpotifyPlaybackClient>();
        _loggerFactory = loggerFactory;
        _playlistClient = playlistClient;
        _timeProvider = timeProvider;
        _config = config;
        _player = player;
        _player.RequestAudioStreamForTrack = RequestAudioStreamForTrack;

        _websocketState.AddRequestHandler("hm://connect-state/v1/player/command", CommandHandler);
        _websocketState.AddMessageHandler("hm://connect-state/v1/connect/volume", VolumeCommandHandler);

        _sessionHolder.Connected
            .SelectMany(async connected =>
            {
                if (connected)
                {
                    _localPlayerState = await _websocketState.ConnectAsync(config.Playback.DeviceName,
                        config.Playback.DeviceType,
                        CancellationToken.None);
                }
                else
                {
                }

                return Unit.Default;
            }).Subscribe();

        // Subscribe to local playback state
        _player.State
            .Throttle(TimeSpan.FromMilliseconds(100))
            .SelectMany(async state =>
            {
                using (await _stateLock.LockAsync())
                {
                    if (state is { } localState)
                    {
                        _lastLocalPlaybackState = localState;
                    }
                    else
                    {
                        _lastLocalPlaybackState = null;
                    }

                    await EvaluatePlaybackState(true);
                }

                return Unit.Default;
            })
            .Subscribe();

        // Subscribe to remote playback state
        _websocketState
            .PlaybackState
            .SelectMany(async state =>
            {
                using (await _stateLock.LockAsync())
                {
                    if (state is { } remoteState && !string.IsNullOrEmpty(remoteState.DeviceId))
                    {
                        _lastRemotePlaybackState = remoteState;
                    }
                    else
                    {
                        _lastRemotePlaybackState = null;
                    }


                    await EvaluatePlaybackState(false);
                }

                return Unit.Default;
            })
            .Subscribe();
    }

    public IObservable<ISpotifyPlaybackState> PlaybackState => _playbackStateSubj;

    private bool _playerInitialized;
    public async Task<Unit> ConnectToRemoteControl(string? deviceName,
        DeviceType? deviceType,
        CancellationToken cancellationToken)
    {
        _config.Playback.DeviceName = deviceName ?? _config.Playback.DeviceName;
        _config.Playback.DeviceType = deviceType ?? _config.Playback.DeviceType;
        if (!_playerInitialized)
        {
            await _player.Initialize();
            _playerInitialized = true;
        }
        await _sessionHolder.EnsureConnectedAsync(true, cancellationToken);
        return Unit.Default;
    }

    public async Task Play(IPlayItemCommandBuilder request, CancellationToken cancellationToken = default)
    {
        using (await _stateLock.LockAsync(cancellationToken))
        {
            var cmd = request.Build();
            if (_lastEmittedPlaybackState is SpotifyLocalPlaybackState localState)
            {
                //TODO:
            }
            else if (_lastEmittedPlaybackState is SpotifyRemotePlaybackState remotePlaybackState)
            {
                await DoCommand(cmd, remotePlaybackState);
            }
        }
    }

    public async Task Pause(CancellationToken cancellationToken = default)
    {
        using (await _stateLock.LockAsync(cancellationToken))
        {
            if (_lastEmittedPlaybackState is SpotifyLocalPlaybackState localState)
            {
                await _player.Pause();
            }
            else if (_lastEmittedPlaybackState is SpotifyRemotePlaybackState remotePlaybackState)
            {
                await DoCommand(SpotifyPauseCommand.Instance, remotePlaybackState);
            }
        }
    }

    public async Task Resume(CancellationToken cancellationToken = default)
    {
        using (await _stateLock.LockAsync(cancellationToken))
        {
            if (_lastEmittedPlaybackState is SpotifyLocalPlaybackState localState)
            {
                await _player.Resume();
            }
            else if (_lastEmittedPlaybackState is SpotifyRemotePlaybackState remotePlaybackState)
            {
                await DoCommand(SpotifyResumeCommand.Instance, remotePlaybackState);
            }
        }
    }

    public async Task Seek(TimeSpan to, CancellationToken cancellationToken = default)
    {
        using (await _stateLock.LockAsync(cancellationToken))
        {
            if (_lastEmittedPlaybackState is SpotifyLocalPlaybackState localState)
            {
                await _player.Seek(to);
            }
            else if (_lastEmittedPlaybackState is SpotifyRemotePlaybackState remotePlaybackState)
            {
                var seekTo = new SeekToCommand(to);
                await DoCommand(seekTo, remotePlaybackState);
            }
        }
    }

    public async Task SkipNext(CancellationToken cancellationToken = default)
    {
        using (await _stateLock.LockAsync(cancellationToken))
        {
            if (_lastEmittedPlaybackState is SpotifyLocalPlaybackState localState)
            {
                await _player.SkipNext();
            }
            else if (_lastEmittedPlaybackState is SpotifyRemotePlaybackState remotePlaybackState)
            {
                var cmd = SkipNextCommand.Instance;
                await DoCommand(cmd, remotePlaybackState);
            }
        }
    }

    public async Task SkipPrevious(CancellationToken cancellationToken = default)
    {
        using (await _stateLock.LockAsync(cancellationToken))
        {
            if (_lastEmittedPlaybackState is SpotifyLocalPlaybackState localState)
            {
                await _player.SkipPrevious();
            }
            else if (_lastEmittedPlaybackState is SpotifyRemotePlaybackState remotePlaybackState)
            {
                var cmd = SkipPreviousCommand.Instance;
                await DoCommand(cmd, remotePlaybackState);
            }
        }
    }

    public async Task SetShuffle(bool shuffle, CancellationToken cancellationToken = default)
    {
        using (await _stateLock.LockAsync(cancellationToken))
        {
            if (_lastEmittedPlaybackState is SpotifyLocalPlaybackState localState)
            {
                await _player.SetShuffle(shuffle);
            }
            else if (_lastEmittedPlaybackState is SpotifyRemotePlaybackState remotePlaybackState)
            {
                var cmd = new SetShuffleCommand(shuffle);
                await DoCommand(cmd, remotePlaybackState);
            }
        }
    }

    public async Task SetRepeatingContext(RepeatMode repeatMode, CancellationToken cancellationToken = default)
    {
        using (await _stateLock.LockAsync(cancellationToken))
        {
            if (_lastEmittedPlaybackState is SpotifyLocalPlaybackState localState)
            {
                await _player.SetRepeatMode(repeatMode);
            }
            else if (_lastEmittedPlaybackState is SpotifyRemotePlaybackState remotePlaybackState)
            {
                var cmd = new SetRepeatCommand(repeatMode);
                await DoCommand(cmd, remotePlaybackState);
            }
        }
    }

    private async Task DoCommand(ISpotifyRemoteCommand command, SpotifyRemotePlaybackState remotePlaybackState)
    {
        var commandDescription = command.Describe();
        _logger.LogInformation("Sending command {Command}", commandDescription);
        var ackid = await _apiClient.DoCommandAsync(_config.Playback.DeviceId,
            remotePlaybackState.DeviceId,
            command, CancellationToken.None);
        if (string.IsNullOrEmpty(ackid))
        {
            _logger.LogWarning("Failed to send command {Command}", command);
            return;
        }

        var tcs = _websocketState.RegisterAckId(ackid);
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await tcs.WaitAsync(cts.Token);
            _logger.LogInformation("Successfully sent command {Command}", commandDescription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send command {Command}", commandDescription);
        }
    }

    private Task<AudioStream?> RequestAudioStreamForTrack(WaveePlayerMediaItem item,
        CancellationToken cancellationToken)
    {
        var audioStream = new WaveeAudioStream(item, item.Id.Value, _audioKeyManager, _apiClient,
            _loggerFactory.CreateLogger<WaveeAudioStream>());
        return Task.FromResult<AudioStream?>(audioStream);
    }

    private Task VolumeCommandHandler(SpotifyWebsocketMessage message,
        IDictionary<string, string> parameters, CancellationToken cancellationtoken)
    {
        //            Connect.SetVolumeCommand cmd = Connect.SetVolumeCommand.parseFrom(payload);
        SetVolumeCommand cmd = SetVolumeCommand.Parser.ParseFrom(message.Payload);
        var asFraction = (float)cmd.Volume / ushort.MaxValue;
        _lastCommandMessageId = cmd.CommandOptions.MessageId;
        _player.SetVolume(asFraction);
        return Task.CompletedTask;
    }

    private async Task CommandHandler(SpotifyWebsocketMessage message, IDictionary<string, string> parameters,
        SpotifyWebsocketRouter.Reply r, CancellationToken cancellationtoken)
    {
        using var jsonDocument = JsonDocument.Parse(message.Payload);
        var messageId = jsonDocument.RootElement.GetProperty("message_id").GetInt32();
        var sentByDeviceId = jsonDocument.RootElement.GetProperty("sent_by_device_id").GetString();
        _lastCommandMessageId = messageId;
        _lastCommandSentByDeviceId = sentByDeviceId;


        var command = jsonDocument.RootElement.GetProperty("command");
        var endpoint = command.GetProperty("endpoint").GetString();
        switch (endpoint)
        {
            case "play":
            {
                await HandlePlayCommand(command);
                break;
            }
            case "skip_prev":
            {
                _logger.LogInformation("Skipping to previous track");
                await _player.SkipPrevious();
                _logger.LogInformation("Skipped to previous track");
                break;
            }
            case "skip_next":
            {
                await HandleSkipNextCommand(command);
                break;
            }
            case "seek_to":
            {
                var seekTo = command.GetProperty("value").GetDouble();
                var seekToTimeSpan = TimeSpan.FromMilliseconds(seekTo);
                _logger.LogInformation("Seeking to {SeekTo}", seekToTimeSpan);
                await _player.Seek(seekToTimeSpan);
                break;
            }
            case "pause":
            {
                _logger.LogInformation("Pausing");
                await _player.Pause();
                break;
            }
            case "resume":
            {
                _logger.LogInformation("Resuming");
                await _player.Resume();
                break;
            }
            case "transfer":
            {
                var transfer = TransferState.Parser.ParseFrom(command.GetProperty("data").GetBytesFromBase64());
                await HandleTransferCommand(transfer);
                break;
            }
            case "add_to_queue":
            {
                //ValueKind = Object : "{"endpoint":"add_to_queue",
                //"track":{"uri":"spotify:track:1Ub5sCBfsF3xGEw4eHqJro",
                //"metadata":{"is_queued":"true"},"provider":"queue"},"logging_params":{"device_identifier":"131ffb02e781cc421714faa0d5dc6e595cc1d713","command_id":"b66bccb3df4404f5512bcd4d38b12db9"}}"
                var track = command.GetProperty("track");
                var uri = track.GetProperty("uri").GetString();
                var metadata = new Dictionary<string, string>();
                if (track.TryGetProperty("metadata", out var metadataElement))
                {
                    foreach (var property in metadataElement.EnumerateObject())
                    {
                        metadata[property.Name] = property.Value.GetString();
                    }
                }

                var trackId = SpotifyId.FromUri(uri);
                var mediaItem = new WaveePlayerMediaItem(trackId, null, metadata);
                await _player.AddToQueue(mediaItem);
                break;
            }
            case "set_repeating_context":
            {
                var value = command.GetProperty("value").GetBoolean();
                if (value)
                {
                    var newRepeatState = RepeatMode.Context;
                    await _player.SetRepeatMode(newRepeatState);
                }
                else
                {
                    await _player.SetRepeatMode(RepeatMode.Off);
                }

                break;
            }
            case "set_repeating_track":
            {
                var value = command.GetProperty("value").GetBoolean();
                if (value)
                {
                    var newRepeatState = RepeatMode.Track;
                    await _player.SetRepeatMode(newRepeatState);
                }

                break;
            }
            case "set_shuffling_context":
            {
                var value = command.GetProperty("value").GetBoolean();
                await _player.SetShuffle(value);
                break;
            }
            default:
            {
                // Log: Warning: Unknown command {Endpoint}
                _logger.LogWarning("Unknown command {Endpoint}", endpoint);
                break;
            }
        }

        await r(message.MessageId.ToString(), true);
    }

    private async Task HandleSkipNextCommand(JsonElement command)
    {
        if (command.TryGetProperty("track", out var trackElement))
        {
            // Extract track information
            var trackUri = trackElement.GetProperty("uri").GetString();
            string? trackUid = null;
            if (trackElement.TryGetProperty("uid", out var uidElement))
            {
                trackUid = uidElement.GetString();
            }

            var metadata = new Dictionary<string, string>();

            if (trackElement.TryGetProperty("metadata", out var metadataElement))
            {
                foreach (var property in metadataElement.EnumerateObject())
                {
                    metadata[property.Name] = property.Value.GetString();
                }
            }

            // Create SpotifyId from URI
            SpotifyId? trackId = null;
            if (!string.IsNullOrEmpty(trackUri))
            {
                trackId = SpotifyId.FromUri(trackUri);
            }

            // Create WaveePlayerMediaItem
            var mediaItem = new WaveePlayerMediaItem(trackId, trackUid);
            foreach (var meta in metadata)
            {
                mediaItem.Metadata.Add(meta.Key, meta.Value);
            }

            // Instruct the player to play the specified track
            _logger.LogInformation("Skipping to specified track: {TrackUri}", trackUri);
            await _player.PlayMediaItemAsync(_player.Context, mediaItem, TimeSpan.Zero);
            _logger.LogInformation("Skipped to specified track");
        }
        else
        {
            // No specific track provided, skip to next track
            _logger.LogInformation("Skipping to next track");
            await _player.SkipNext();
            _logger.LogInformation("Skipped to next track");
        }
    }

    private async Task HandlePlayCommand(JsonElement command)
    {
        var contextRoot = command.GetProperty("context");
        var contextAsContext = Context.Parser.ParseJson(contextRoot.GetRawText());

        var play_origin = command.GetProperty("play_origin");
        var playOrigin = new PlayOrigin();
        if (play_origin.TryGetProperty("feature_identifier", out var featureIdentifier))
        {
            playOrigin.FeatureIdentifier = featureIdentifier.GetString();
        }

        if (play_origin.TryGetProperty("feature_version", out var featureVersion))
        {
            playOrigin.FeatureVersion = featureVersion.GetString();
        }

        if (play_origin.TryGetProperty("referrer_identifier", out var referrerIdentifier))
        {
            playOrigin.ReferrerIdentifier = referrerIdentifier.GetString();
        }

        if (play_origin.TryGetProperty("device_identifier", out var deviceIdentifier))
        {
            playOrigin.DeviceIdentifier = deviceIdentifier.GetString();
        }

        // check if the context is the same
        var contextIsSame = _player.Context?.Id == contextAsContext.Uri;
        WaveePlayerPlaybackContext? contextObj = _player.Context;
        if (!contextIsSame)
        {
            contextObj = CreateContextFrom(contextAsContext);
        }
        else if (_player.Context is WaveePlaylistPlaybackContext playlistPlaybackContext)
        {
            string? sortingCriteria = null;
            if (contextAsContext.Metadata.TryGetValue("sorting.criteria", out var sortingCriteriaValue))
            {
                sortingCriteria = sortingCriteriaValue;
            }

            await playlistPlaybackContext.UpdateSortingCriteria(sortingCriteria);
        }
        else if (contextObj.Id.StartsWith("spotify:album"))
        {
            if (contextAsContext.Pages.Count > 0)
            {
                (contextObj as WaveeRegularPlaybackContext)!.LoadPages(contextAsContext.Pages);
            }
        }

        var options = command.GetProperty("options");
        var skip_to = options.GetProperty("skip_to");
        _playOrigin = playOrigin;
        if (skip_to.TryGetProperty("track_uid", out var trUid))
        {
            var track_uid = trUid.GetString();
            SpotifyId? trackId = null;
            if (skip_to.TryGetProperty("track_uri", out var trId))
            {
                trackId = SpotifyId.FromUri(trId.GetString());
            }

            var mediaItem = new WaveePlayerMediaItem(trackId, track_uid);
            await _player.PlayMediaItemAsync(contextObj, mediaItem, TimeSpan.Zero);
        }
        else if (skip_to.TryGetProperty("track_index", out var trIndex) && trIndex.ValueKind is JsonValueKind.Number)
        {
            if (skip_to.TryGetProperty("track_uri", out var trIndex2))
            {
                var trackUri = trIndex2.GetString();
                var trackId = SpotifyId.FromUri(trackUri);
                string? track_uid = null;
                if (skip_to.TryGetProperty("track_uid", out var trUidMetadata))
                {
                    track_uid = trUid.GetString();
                }

                var mediaItem = new WaveePlayerMediaItem(trackId, track_uid);
                await _player.PlayMediaItemAsync(contextObj, mediaItem, TimeSpan.Zero);
            }
            else
            {
                int pageIndex = 0;
                if (skip_to.TryGetProperty("page_index", out var pgIndex) && pgIndex.ValueKind is JsonValueKind.Number)
                {
                    pageIndex = pgIndex.GetInt32();
                }

                var trackIndex = trIndex.GetInt32();
                await _player.PlayMediaItemAsync(contextObj!, pageIndex, trackIndex);
            }
        }
        else if (skip_to.TryGetProperty("track_uri", out var trIndex2))
        {
            var trackUri = trIndex2.GetString();
            var trackId = SpotifyId.FromUri(trackUri);
            string? track_uid = null;
            if (skip_to.TryGetProperty("track_uid", out var trUidMetadata))
            {
                track_uid = trUid.GetString();
            }

            var mediaItem = new WaveePlayerMediaItem(trackId, track_uid);
            await _player.PlayMediaItemAsync(contextObj, mediaItem, TimeSpan.Zero);
        }
        else
        {
            await _player.PlayMediaItemAsync(contextObj!, 0, 0);
        }
    }

    private async Task HandleTransferCommand(TransferState transfer)
    {
        var trackId = transfer.Playback.CurrentTrack.Gid;
        var spotifyId = SpotifyId.FromGid(trackId, SpotifyItemType.Track);
        var mediaItem = new WaveePlayerMediaItem(spotifyId, transfer.Playback.CurrentTrack.Uid);
        var positionAsOfTimestamp = transfer.Playback.PositionAsOfTimestamp;
        var timestamp = transfer.Playback.Timestamp;
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var position = positionAsOfTimestamp + (now - timestamp);
        var posTimeSpan = TimeSpan.FromMilliseconds(position);
        if (_lastRemotePlaybackState != null)
            _lastRemotePlaybackState.DeviceId = _config.Playback.DeviceId;
        _playOrigin = new PlayOrigin
        {
            FeatureIdentifier = transfer.CurrentSession.PlayOrigin.FeatureIdentifier,
            ReferrerIdentifier = transfer.CurrentSession.PlayOrigin.ReferrerIdentifier,
            ExternalReferrer = transfer.CurrentSession.PlayOrigin.ExternalReferrer,
            ViewUri = transfer.CurrentSession.PlayOrigin.ViewUri,
            DeviceIdentifier = transfer.CurrentSession.PlayOrigin.DeviceIdentifier,
            FeatureClasses = { transfer.CurrentSession.PlayOrigin.FeatureClasses },
            FeatureVersion = transfer.CurrentSession.PlayOrigin.FeatureVersion,
        };
        var ctx = transfer.CurrentSession.Context;
        var context = CreateContextFrom(ctx);
        if (context is WaveePlaylistPlaybackContext playlistPlaybackContext)
        {
            string? sortingCriteria = null;
            if (ctx.Metadata.TryGetValue("sorting.criteria", out var sortingCriteriaValue))
            {
                sortingCriteria = sortingCriteriaValue;
            }

            await playlistPlaybackContext.UpdateSortingCriteria(sortingCriteria);
        }

        var shuffling = transfer.CurrentSession.OptionOverrides?.ShufflingContext ?? false;
        var repeatContext = transfer.CurrentSession.OptionOverrides?.RepeatingContext ?? false;
        var repeatTrack = transfer.CurrentSession.OptionOverrides?.RepeatingTrack ?? false;
        var repeatMode = repeatTrack ? RepeatMode.Track : repeatContext ? RepeatMode.Context : RepeatMode.Off;
        await _player.PlayMediaItemAsync(context,
            mediaItem, posTimeSpan, shuffling, repeatMode);
    }

    private WaveePlayerPlaybackContext CreateContextFrom(Context context)
    {
        string contextUri = context.Uri;

        if (contextUri.StartsWith("spotify:playlist"))
        {
            //added_at DESC, album_title, album_artist_name, album_disc_number, album_track_number
            var metadata = context.Metadata;
            string? sortingCriteria = null;
            if (metadata.TryGetValue("sorting.criteria", out var sortingCriteriaValue))
            {
                sortingCriteria = sortingCriteriaValue;
            }

            var playlistContext = new WaveePlaylistPlaybackContext(contextUri,
                _playlistClient,
                _apiClient,
                _loggerFactory.CreateLogger<IWaveePlayer>());
            return playlistContext;
        }

        var ctx = new WaveeRegularPlaybackContext(contextUri, _apiClient, _loggerFactory.CreateLogger<IWaveePlayer>());
        if (context.Uri.StartsWith("spotify:album"))
        {
            var pages = context.Pages;
            if (context.Pages.Count > 0)
            {
                ctx.LoadPages(pages);
            }
        }

        return ctx;
    }

    private readonly AsyncLock _stateLockUpdate = new();
    private readonly ITimeProvider _timeProvider;

    private async Task EvaluatePlaybackState(bool changedBecauseOfLocalPlayback, bool alwaysPlay = false)
    {
        using (await _stateLockUpdate.LockAsync())
        {
            var localDeviceId = _config.Playback.DeviceId;

            if (_lastLocalPlaybackState != null)
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var startedPlayingAt = _lastLocalPlaybackState.PlayingSinceTimestamp;
                var ts = _lastRemotePlaybackState?.ClusterTimestamp.ToUnixTimeMilliseconds() - 3000;
                var nowIsGreaterThanStartedPlayingAt = now > startedPlayingAt?.ToUnixTimeMilliseconds();
                var tsIsGreaterThanStartedPlayingAt = ts > startedPlayingAt?.ToUnixTimeMilliseconds();

                // Local playback is active
                if (
                    !alwaysPlay &&
                    _lastRemotePlaybackState != null &&
                    _lastRemotePlaybackState.DeviceId != localDeviceId &&
                    nowIsGreaterThanStartedPlayingAt &&
                    tsIsGreaterThanStartedPlayingAt)
                {
                    // long ts = update.getCluster().getTimestamp() - 3000; // Workaround
                    //if (!session.deviceId().equals(update.getCluster().getActiveDeviceId()) && isActive() && now > startedPlayingAt() && ts > startedPlayingAt())
                    // Remote playback is active on another devic
                    // Stop local playback
                    _player.Stop();
                    _lastLocalPlaybackState = null;
                    // Emit remote playback state
                    EmitPlaybackState(_lastRemotePlaybackState);
                }
                else
                {
                    // No remote playback or remote playback is on our device
                    EmitPlaybackState(_lastLocalPlaybackState);

                    if (changedBecauseOfLocalPlayback)
                    {
                        // Notify Spotify API of the new local playback state
                        await NotifySpotifyOfLocalPlaybackAsync(_lastLocalPlaybackState);
                    }
                }
            }
            else
            {
                // Local playback is not active
                if (_lastRemotePlaybackState != null && !string.IsNullOrEmpty(_lastRemotePlaybackState.DeviceId))
                {
                    // Remote playback is active on another device
                    EmitPlaybackState(_lastRemotePlaybackState);
                }
                else
                {
                    // No playback on either device
                    // Emit NonePlaybackState with last known state
                    var noneState = NonePlaybackState.Instance;
                    EmitPlaybackState(noneState);
                }
            }
        }
    }

    private void EmitPlaybackState(ISpotifyPlaybackState newState)
    {
        _lastEmittedPlaybackState = newState;
        _playbackStateSubj.OnNext(newState);
    }

    private async Task NotifySpotifyOfLocalPlaybackAsync(SpotifyLocalPlaybackState localState)
    {
        try
        {
            if (localState.PlayingSinceTimestamp is null && _localPlayerState is not null)
            {
                var emptyState = new PutStateRequest();
                _localPlayerState.PlayerState = _websocketState.NewState().PlayerState;
                emptyState.Device = _localPlayerState;
                emptyState.PutStateReason = PutStateReason.BecameInactive;
                emptyState.MemberType = MemberType.ConnectState;
                await _apiClient.PutConnectState(_config.Playback.DeviceId,
                    _websocketState.ConnectionId,
                    emptyState,
                    CancellationToken.None);
                return;
            }

            var now = await _timeProvider.CurrentTime();
            var pos = (long)localState.Position.TotalMilliseconds;
            _localPlayerState.DeviceInfo.Volume = (uint)(_player.Volume * ushort.MaxValue);
            _localPlayerState.PlayerState.Duration = (long)localState.TotalDuration.TotalMilliseconds;
            _localPlayerState.PlayerState.PositionAsOfTimestamp = pos;
            _localPlayerState.PlayerState.Position = 0;

            _localPlayerState.PlayerState.Timestamp = now.ToUnixTimeMilliseconds();
            _localPlayerState.PlayerState.PlaybackSpeed = localState.IsPaused ? 0 : 1;
            _localPlayerState.PlayerState.IsPaused = localState.IsPaused;
            _localPlayerState.PlayerState.IsBuffering = localState.IsBuffering;
            _localPlayerState.PlayerState.IsPlaying = true;
            _localPlayerState.PlayerState.PlayOrigin = _playOrigin;
            _localPlayerState.PlayerState.Options = new ContextPlayerOptions();

            switch (localState.RepeatState)
            {
                case RepeatMode.Off:
                    _localPlayerState.PlayerState.Options.RepeatingContext = false;
                    _localPlayerState.PlayerState.Options.RepeatingTrack = false;
                    break;
                case RepeatMode.Context:
                    _localPlayerState.PlayerState.Options.RepeatingContext = true;
                    _localPlayerState.PlayerState.Options.RepeatingTrack = false;
                    break;
                case RepeatMode.Track:
                    _localPlayerState.PlayerState.Options.RepeatingContext = true;
                    _localPlayerState.PlayerState.Options.RepeatingTrack = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _localPlayerState.PlayerState.Options.ShufflingContext = localState.IsShuffling;
            _localPlayerState.PlayerState.ContextUrl = localState.ContextUrl;

            _localPlayerState.PlayerState.Restrictions = new Restrictions();
            _localPlayerState.PlayerState.ContextRestrictions = new Restrictions();
            _localPlayerState.PlayerState.Track = new ProvidedTrack();
            _localPlayerState.PlayerState.Track.Uri = localState.CurrentTrackId.ToString();
            if (localState.CurrentTrackMetadata is not null)
            {
                _localPlayerState.PlayerState.Track.Metadata.Clear();
                foreach (var meta in localState.CurrentTrackMetadata)
                {
                    _localPlayerState.PlayerState.Track.Metadata.Add(meta.Key, meta.Value);
                }
            }

            _localPlayerState.PlayerState.NextTracks.Clear();
            _localPlayerState.PlayerState.PrevTracks.Clear();
            foreach (var next in await _player.GetUpcomingTracksAsync(50, CancellationToken.None))
            {
                var providedTrack = new ProvidedTrack
                {
                    Uri = next.Id.ToString(),
                };
                if (!string.IsNullOrEmpty(next.Uid))
                {
                    providedTrack.Uid = next.Uid;
                }

                foreach (var meta in next.Metadata)
                {
                    providedTrack.Metadata.Add(meta.Key, meta.Value);
                }

                if (next.IsQueued)
                {
                    providedTrack.Provider = "queue";
                    providedTrack.Uid = $"q{next.QueueId ?? 0}";
                }
                else
                {
                    providedTrack.Provider = "context";
                }

                providedTrack.Restrictions = new Restrictions();
                _localPlayerState.PlayerState.NextTracks.Add(providedTrack);
            }

            foreach (var played in await _player.GetPreviousTracksInCOntextAsync(1000, CancellationToken.None))
            {
                var providedTrack = new ProvidedTrack
                {
                    Uri = played.Id.ToString(),
                };
                if (!string.IsNullOrEmpty(played.Uid))
                {
                    providedTrack.Uid = played.Uid;
                }

                foreach (var meta in played.Metadata)
                {
                    providedTrack.Metadata.Add(meta.Key, meta.Value);
                }

                providedTrack.Provider = "context";
                providedTrack.Restrictions = new Restrictions();
                _localPlayerState.PlayerState.PrevTracks.Insert(0, providedTrack);
            }


            if (!string.IsNullOrEmpty(localState.CurrentTrackUid))
            {
                _localPlayerState.PlayerState.Track.Uid = localState.CurrentTrackUid;
            }

            _localPlayerState.PlayerState.ContextUrl = localState.ContextUrl;
            _localPlayerState.PlayerState.ContextUri = localState.ContextUri;

            var putState = new PutStateRequest();
            putState.Device = _localPlayerState;
            putState.PutStateReason = PutStateReason.PlayerStateChanged;
            putState.MemberType = MemberType.ConnectState;

            if (_lastCommandSentByDeviceId != null)
            {
                putState.LastCommandSentByDeviceId = _lastCommandSentByDeviceId;
            }

            if (_lastCommandMessageId != null)
            {
                putState.LastCommandMessageId = (uint)_lastCommandMessageId.Value;
            }

            putState.IsActive = true;
            putState.HasBeenPlayingForMs = (ulong)pos;
            putState.StartedPlayingAt = (ulong)localState.PlayingSinceTimestamp.Value.ToUnixTimeMilliseconds();
            putState.ClientSideTimestamp = (ulong)now.ToUnixTimeMilliseconds();

            if (!string.IsNullOrEmpty(_websocketState.ConnectionId))
            {
                _websocketState.NewPutStateRequest(putState);
                var newRemoteState = await _apiClient.PutConnectState(_config.Playback.DeviceId,
                    _websocketState.ConnectionId,
                    putState,
                    CancellationToken.None);

                // Log: Notified Spotify that we are playing {TrackId} and are at {Position}
                _logger.LogInformation("Notified Spotify that we are playing {TrackId} and are at {Position}",
                    localState.CurrentTrackId, localState.Position);
                await _websocketState.ForceNewClusterUpdate(newRemoteState);
            }
            else
            {
                _logger.LogWarning("Cannot notify Spotify of local playback state, no websocket connection.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify Spotify of local playback state.");
        }
    }
}