// using System.Diagnostics;
// using System.Reactive.Subjects;
// using System.Threading.Channels;
// using Microsoft.Extensions.Logging;
// using NAudio.Wave;
// using NAudio.Wave.SampleProviders;
// using NeoSmart.AsyncLock;
// using Wavee.Config;
// using Wavee.Enums;
// using Wavee.Interfaces;
// using Wavee.Models.Common;
// using Wavee.Playback.Contexts;
// using Wavee.Playback.Streaming;
// using Wavee.Services.Playback;
//
// namespace Wavee.Playback.Player;
//
// public sealed class WaveeKnownPlaybackException : Exception
// {
//     public WaveeKnownPlaybackException(WaveeKnownPlaybackError error)
//         : base(error.ToString())
//     {
//         Error = error;
//     }
//
//     public WaveeKnownPlaybackError Error { get; }
// }
//
// public enum WaveeKnownPlaybackError
// {
//     NoMoreTracks
// }
//
// public sealed class WaveePlayer : IWaveePlayer, IAsyncDisposable
// {
//     private readonly AsyncLock _lock = new();
//     private readonly ILogger<WaveePlayer> _logger;
//     private readonly SpotifyConfig _config;
//     private readonly ITimeProvider _timeProvider;
//     private readonly AudioOutput _audioOutput;
//     private readonly ReplaySubject<SpotifyLocalPlaybackState?> _stateSubject = new();
//     private readonly CancellationTokenSource _cancellationTokenSource = new();
//
//     private readonly Channel<PlayerCommand> _commandChannel = Channel.CreateUnbounded<PlayerCommand>(
//         new UnboundedChannelOptions
//         {
//             SingleReader = true,
//             SingleWriter = false
//         });
//
//     // Playback state variables
//     private DateTimeOffset? _playbackStartedAt = null;
//     private TrackQueue _trackQueue = new();
//     private Task<(AudioStream, TimeSpan)>? _streamTask = null;
//     private ISampleProviderExtended? _extended = null;
//     private ISampleProvider? _sampleProvider = null;
//     private AudioStream? _activeStream = null;
//     private Guid? _playbackId = null;
//     private bool _isPaused = false;
//
//     // Volume control
//     private float _volume = 1f;
//
//     // Playback context
//     public WaveePlayerPlaybackContext? Context => _trackQueue.Context;
//
//
//     // Delegate for requesting audio streams
//     public RequestAudioStreamForTrackAsync? RequestAudioStreamForTrack { get; set; }
//
//     public IObservable<SpotifyLocalPlaybackState?> State => _stateSubject;
//
//     public float Volume
//     {
//         get => _volume;
//         set
//         {
//             _volume = value;
//             _sampleProvider = _sampleProvider?.WithVolume(_volume);
//         }
//     }
//
//     public TimeSpan Position => _extended?.CurrentTime ?? TimeSpan.Zero;
//
//     public WaveePlayer(
//         AudioOutput audioOutput,
//         ILogger<WaveePlayer> logger,
//         SpotifyConfig config,
//         ITimeProvider timeProvider)
//     {
//         _audioOutput = audioOutput;
//         _logger = logger;
//         _config = config;
//         _timeProvider = timeProvider;
//
//         InitializePlayerLoop();
//     }
//
//     private void InitializePlayerLoop()
//     {
//         // Start the playback and command processing loops
//         _ = Task.WhenAll(
//             PlaybackLoopAsync(_cancellationTokenSource.Token),
//             CommandLoopAsync(_cancellationTokenSource.Token));
//     }
//
//     private async Task PlaybackLoopAsync(CancellationToken cancellationToken)
//     {
//         try
//         {
//             SpotifyLocalPlaybackState? currentPlaybackState =
//                 NonePlaybackState.Instance as SpotifyLocalPlaybackState;
//             EmitPlaybackState(currentPlaybackState);
//
//             Guid? previousPlaybackId = null;
//             TimeSpan? previousDuration = null;
//             TimeSpan? seekTo = null;
//
//             const int BufferSize = 4096;
//
//             while (!cancellationToken.IsCancellationRequested)
//             {
//                 try
//                 {
//                     if (_activeStream == null && _streamTask == null)
//                     {
//                         await Task.Delay(10, cancellationToken);
//                         continue;
//                     }
//
//                     _playbackStartedAt ??= _timeProvider.CurrentTime().Result;
//
//                     if (_streamTask != null && _streamTask.IsCompleted)
//                     {
//                         using (await _lock.LockAsync())
//                         {
//                             _activeStream?.Dispose();
//                             _extended = null;
//                             _sampleProvider = null;
//
//                             var (activeStreamResult, seekToResult) = await _streamTask;
//                             _activeStream = activeStreamResult;
//                             seekTo = seekToResult != TimeSpan.Zero ? seekToResult : null;
//
//                             _streamTask = null;
//                             _playbackId = Guid.NewGuid();
//                         }
//
//                         _audioOutput.Clear();
//                         _audioOutput.Resume();
//
//                         // Update playback state to reflect new track
//                         currentPlaybackState = CreatePlaybackState(
//                             _activeStream.MediaItem,
//                             _playbackStartedAt,
//                             position: TimeSpan.Zero,
//                             paused: false,
//                             _activeStream.Track,
//                             duration: _activeStream.Track?.Duration,
//                             Context);
//                         EmitPlaybackState(currentPlaybackState);
//                     }
//
//                     var stream = _activeStream;
//                     if (stream == null)
//                     {
//                         await Task.Delay(1, cancellationToken);
//                         continue;
//                     }
//
//                     if (_extended == null)
//                     {
//                         using (await _lock.LockAsync())
//                         {
//                             try
//                             {
//                                 _sampleProvider = CreateSampleProvider(stream, out _extended, _volume);
//                                 if (seekTo != null)
//                                 {
//                                     _extended.Seek(seekTo.Value);
//                                     seekTo = null;
//                                 }
//                             }
//                             catch (Exception ex)
//                             {
//                                 _logger.LogError(ex, "Error creating sample provider. Skipping to next track.");
//                                 await EnqueueCommandAsync(new SkipNextCommand());
//                                 continue;
//                             }
//                         }
//                     }
//
//                     if (_isPaused)
//                     {
//                         _audioOutput.Pause();
//                         await Task.Delay(100, cancellationToken);
//                         continue;
//                     }
//                     else
//                     {
//                         _audioOutput.Resume();
//                     }
//
//                     // Update playback state if necessary
//                     var duration = stream.TotalDuration;
//                     if (previousDuration != duration || _playbackId != previousPlaybackId)
//                     {
//                         if (previousDuration != duration)
//                         {
//                             previousDuration = duration;
//                         }
//
//                         if (_playbackId != previousPlaybackId)
//                         {
//                             previousPlaybackId = _playbackId;
//                         }
//
//                         var currentTime = _extended.CurrentTime;
//                         currentPlaybackState = CreatePlaybackState(
//                             stream.MediaItem,
//                             _playbackStartedAt,
//                             position: currentTime,
//                             paused: false,
//                             stream.Track,
//                             duration: duration,
//                             Context);
//                         EmitPlaybackState(currentPlaybackState);
//                     }
//
//                     float[] buffer = new float[BufferSize];
//                     var read = _sampleProvider!.Read(buffer, 0, buffer.Length);
//                     if (read == 0)
//                     {
//                         await HandleTrackEndAsync(cancellationToken);
//                         continue;
//                     }
//
//                     _audioOutput.Write(buffer.AsSpan(0, read));
//                 }
//                 catch (OperationCanceledException)
//                 {
//                     _logger.LogInformation("Playback loop canceled.");
//                     break;
//                 }
//                 catch (Exception ex)
//                 {
//                     _logger.LogError(ex, "An error occurred in the playback loop.");
//                 }
//             }
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "An unexpected error occurred in PlaybackLoopAsync.");
//         }
//         finally
//         {
//             _cancellationTokenSource.Dispose();
//         }
//     }
//
//     private async Task HandleTrackEndAsync(CancellationToken cancellationToken)
//     {
//         _sampleProvider = null;
//         _extended = null;
//         _activeStream?.Dispose();
//         _activeStream = null;
//
//         await EnqueueCommandAsync(new SkipNextCommand());
//
//         // Wait until audio output consumes remaining data
//         _audioOutput.Consume();
//     }
//
//     private async Task CommandLoopAsync(CancellationToken cancellationToken)
//     {
//         try
//         {
//             await foreach (var command in _commandChannel.Reader.ReadAllAsync(cancellationToken))
//             {
//                 try
//                 {
//                     await ProcessCommandAsync(command, cancellationToken);
//                     command.Complete();
//                 }
//                 catch (Exception ex)
//                 {
//                     _logger.LogError(ex, "Error processing command {CommandType}.", command.GetType().Name);
//                     command.Error(ex);
//                 }
//             }
//         }
//         catch (OperationCanceledException)
//         {
//             _logger.LogInformation("Command loop canceled.");
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "An unexpected error occurred in the command loop.");
//         }
//         finally
//         {
//             _logger.LogInformation("Command loop exited.");
//         }
//     }
//
//     private async Task ProcessCommandAsync(PlayerCommand command, CancellationToken cancellationToken)
//     {
//         switch (command)
//         {
//             case AddToQueueCommand addCmd:
//                 await HandleAddToQueueAsync(addCmd, cancellationToken);
//                 break;
//
//             case SkipPrevCommand _:
//                 await HandleSkipPreviousAsync(cancellationToken);
//                 break;
//
//             case SkipNextCommand _:
//                 await HandleSkipNextAsync(cancellationToken);
//                 break;
//
//             case PlayTrackAtIndexCommand playAtIndexCmd:
//                 await HandlePlayTrackAtIndexAsync(playAtIndexCmd, cancellationToken);
//                 break;
//
//             case PlayTrackCommand playCmd:
//                 await HandlePlayTrackAsync(playCmd, cancellationToken);
//                 break;
//
//             case PauseCommand _:
//                 HandlePause();
//                 break;
//
//             case ResumeCommand _:
//                 HandleResume();
//                 break;
//
//             case SetShuffleCommand shuffleCmd:
//                 await HandleSetShuffleAsync(shuffleCmd, cancellationToken);
//                 break;
//
//             case SetRepeatModeCommand repeatCmd:
//                 await HandleSetRepeatModeAsync(repeatCmd, cancellationToken);
//                 break;
//
//             case SeekCommand seekCmd:
//                 await HandleSeekAsync(seekCmd, cancellationToken);
//                 break;
//
//             default:
//                 _logger.LogWarning("Unknown command type: {CommandType}.", command.GetType().Name);
//                 break;
//         }
//     }
//
//     private async Task HandleAddToQueueAsync(AddToQueueCommand command, CancellationToken cancellationToken)
//     {
//         using (await _lock.LockAsync())
//         {
//             if (_trackQueue == null)
//             {
//                 _logger.LogWarning("Track queue is not initialized.");
//             }
//             else
//             {
//                 _trackQueue.Enqueue(command.MediaItem);
//             }
//
//             EmitCurrentPlaybackState();
//         }
//     }
//
//     private Task HandleSkipPreviousAsync(CancellationToken cancellationToken)
//     {
//         // TODO: Implement skipping to the previous track
//         _logger.LogInformation("Skip previous not implemented yet.");
//         throw new NotImplementedException("Skip previous functionality is not implemented.");
//     }
//
//     private async Task HandleSkipNextAsync(CancellationToken cancellationToken)
//     {
//         _logger.LogInformation("Skipping to next track.");
//         using (await _lock.LockAsync())
//         {
//             if (_trackQueue == null)
//             {
//                 _logger.LogWarning("Track queue is not initialized.");
//             }
//             else
//             {
//                 _audioOutput.Stop();
//                 _audioOutput.Clear();
//                 _extended = null;
//                 _sampleProvider = null;
//                 var nextItem = await _trackQueue.NextItem(true, cancellationToken);
//                 if (nextItem == null)
//                 {
//                     _logger.LogWarning("No more tracks to play.");
//                     throw new WaveeKnownPlaybackException(WaveeKnownPlaybackError.NoMoreTracks);
//                 }
//                 else
//                 {
//                     _logger.LogInformation("Playing next track: {TrackName}", nextItem);
//                     _streamTask = RequestAndInitializeStreamAsync(nextItem, cancellationToken);
//                 }
//             }
//         }
//     }
//
//     private async Task HandlePlayTrackAtIndexAsync(PlayTrackAtIndexCommand command,
//         CancellationToken cancellationToken)
//     {
//         using (await _lock.LockAsync())
//         {
//             if (Context != command.Context)
//             {
//                 await _trackQueue.Initialize(command.Context, cancellationToken);
//             }
//
//             var itemAtIndex = await _trackQueue.SetStartingIndex(
//                 command.PageIndex,
//                 command.TrackIndex,
//                 cancellationToken);
//
//             if (itemAtIndex != null)
//             {
//                 var newCommand = new PlayTrackCommand(itemAtIndex, command.Context, TimeSpan.Zero);
//                 await EnqueueCommandAsync(newCommand);
//             }
//         }
//     }
//
//     private async Task HandlePlayTrackAsync(PlayTrackCommand command, CancellationToken cancellationToken)
//     {
//         using (await _lock.LockAsync())
//         {
//             if (Context != command.Context)
//             {
//                 // Initialize new context and track queue
//                 await _trackQueue.Initialize(command.Context, cancellationToken);
//                 await _trackQueue.SetStartingItem(command.MediaItem, cancellationToken);
//             }
//             else if (Context is WaveePlaylistPlaybackContext currentPlaylist &&
//                      command.Context is WaveePlaylistPlaybackContext newPlaylist &&
//                      currentPlaylist.SortingCriteria != newPlaylist.SortingCriteria)
//             {
//                 await _trackQueue.Initialize(newPlaylist, cancellationToken);
//                 await _trackQueue.SetStartingItem(command.MediaItem, cancellationToken);
//             }
//             else
//             {
//                 await _trackQueue.SetStartingItem(command.MediaItem, cancellationToken);
//             }
//
//             _streamTask = RequestAndInitializeStreamAsync(command.MediaItem, cancellationToken, command.StartFrom);
//         }
//     }
//
//     private void HandlePause()
//     {
//         _logger.LogInformation("Pausing playback.");
//         _audioOutput.Pause();
//         _isPaused = true;
//         EmitCurrentPlaybackState(paused: true);
//     }
//
//     private void HandleResume()
//     {
//         _logger.LogInformation("Resuming playback.");
//         _audioOutput.Resume();
//         _isPaused = false;
//         EmitCurrentPlaybackState(paused: false);
//     }
//
//     private async Task HandleSetShuffleAsync(SetShuffleCommand command, CancellationToken cancellationToken)
//     {
//         using (await _lock.LockAsync())
//         {
//             _trackQueue.ToggleShuffle(command.Value);
//         }
//
//         EmitCurrentPlaybackState();
//     }
//
//     private async Task HandleSetRepeatModeAsync(SetRepeatModeCommand command, CancellationToken cancellationToken)
//     {
//         using (await _lock.LockAsync())
//         {
//             _trackQueue.SetRepeatMode(command.Mode);
//         }
//
//         EmitCurrentPlaybackState();
//     }
//
//     private async Task HandleSeekAsync(SeekCommand command, CancellationToken cancellationToken)
//     {
//         _logger.LogInformation("Seeking to position: {Position}", command.Position);
//         using (await _lock.LockAsync())
//         {
//             if (_extended != null)
//             {
//                 _extended.Seek(command.Position);
//                 _extended.Clear();
//                 _audioOutput.Clear();
//             }
//         }
//
//         EmitCurrentPlaybackState(position: command.Position);
//     }
//
//     private async Task<(AudioStream, TimeSpan)> RequestAndInitializeStreamAsync(
//         WaveePlayerMediaItem mediaItem,
//         CancellationToken cancellationToken,
//         TimeSpan startFrom = default)
//     {
//         if (RequestAudioStreamForTrack == null)
//         {
//             throw new InvalidOperationException("RequestAudioStreamForTrack delegate is not set.");
//         }
//
//         var stream = await RequestAudioStreamForTrack(mediaItem, cancellationToken);
//         await stream.InitializeAsync(cancellationToken);
//         return (stream, startFrom);
//     }
//
//     private ISampleProvider CreateSampleProvider(
//         AudioStream audioStream,
//         out ISampleProviderExtended extended,
//         float volume)
//     {
//         extended = audioStream.CreateSampleProvider();
//
//         var sampleProvider = ApplyEqualizer(extended);
//         sampleProvider = ApplyNormalization(sampleProvider, 1f);
//
//         var globalVolumeSampleProvider = new VolumeSampleProvider(sampleProvider)
//         {
//             Volume = volume
//         };
//
//         return globalVolumeSampleProvider;
//     }
//
//     private ISampleProvider ApplyEqualizer(ISampleProvider input)
//     {
//         // Implement equalizer logic here if needed
//         return input;
//     }
//
//     private ISampleProvider ApplyNormalization(ISampleProvider input, float target)
//     {
//         // Implement normalization logic here if needed
//         return input;
//     }
//
//     private void EmitPlaybackState(SpotifyLocalPlaybackState? state)
//     {
//         _stateSubject.OnNext(state);
//     }
//
//     private void EmitCurrentPlaybackState(
//         WaveePlayerMediaItem? mediaItem = null,
//         DateTimeOffset? playingSince = null,
//         TimeSpan position = default,
//         bool? paused = null,
//         SpotifyPlayableItem? currentTrack = null,
//         TimeSpan? duration = null,
//         WaveePlayerPlaybackContext? context = null)
//     {
//         var playbackState = CreatePlaybackState(
//             mediaItem ?? _activeStream?.MediaItem,
//             playingSince ?? _playbackStartedAt,
//             position: position == default ? (_extended?.CurrentTime ?? TimeSpan.Zero) : position,
//             paused: paused ?? _isPaused,
//             currentTrack: currentTrack ?? _activeStream?.Track,
//             duration: duration ?? _activeStream?.Track?.Duration,
//             context: context ?? Context);
//         EmitPlaybackState(playbackState);
//     }
//
//     private SpotifyLocalPlaybackState CreatePlaybackState(
//         WaveePlayerMediaItem? mediaItem,
//         DateTimeOffset? playingSince,
//         TimeSpan position,
//         bool paused,
//         SpotifyPlayableItem? currentTrack,
//         TimeSpan? duration,
//         WaveePlayerPlaybackContext? context)
//     {
//         var stopwatch = new Stopwatch();
//         if (!paused)
//         {
//             stopwatch.Start();
//         }
//
//         var finalDuration = duration ?? currentTrack?.Duration;
//         string contextUrl = string.Empty;
//         string contextId = string.Empty;
//
//         if (context != null)
//         {
//             contextId = context.Id;
//             contextUrl = $"context://{contextId}";
//         }
//
//         return new SpotifyLocalPlaybackState(
//             playingSince: playingSince,
//             deviceId: _config.Playback.DeviceId,
//             deviceName: _config.Playback.DeviceName,
//             isPaused: paused,
//             isBuffering: finalDuration == null,
//             trackId: mediaItem?.Id ?? new SpotifyId(),
//             trackUid: mediaItem?.Uid ?? string.Empty,
//             positionSinceSw: position,
//             stopwatch: stopwatch,
//             totalDuration: finalDuration ?? TimeSpan.FromMinutes(3),
//             repeatState: _trackQueue.RepeatMode,
//             isShuffling: _trackQueue.Shuffle,
//             contextUrl: contextUrl,
//             contextUri: contextId,
//             currentTrack: currentTrack,
//             currentTrackMetadata: mediaItem?.Metadata
//         );
//     }
//
//     public async Task StopAsync()
//     {
//         _audioOutput.Stop();
//         _streamTask = null;
//         _activeStream?.Dispose();
//         _activeStream = null;
//         _sampleProvider = null;
//         _extended = null;
//         _playbackStartedAt = null;
//         _playbackId = null;
//         _isPaused = false;
//         _trackQueue.Reset();
//         EmitPlaybackState(NonePlaybackState.Instance as SpotifyLocalPlaybackState);
//         await Task.CompletedTask;
//     }
//
//     public Task Stop()
//     {
//         throw new NotImplementedException();
//     }
//
//     public Task Pause()
//     {
//         var command = new PauseCommand();
//         return EnqueueCommandAsync(command);
//     }
//
//     public Task Resume()
//     {
//         var command = new ResumeCommand();
//         return EnqueueCommandAsync(command);
//     }
//
//     public Task Seek(TimeSpan to)
//     {
//         var command = new SeekCommand(to);
//         return EnqueueCommandAsync(command);
//     }
//
//     public Task SetVolume(float volume)
//     {
//         Volume = volume;
//         //_audioOutput.SetVolume(volume); // Uncomment if AudioOutput supports setting volume directly
//         return Task.CompletedTask;
//     }
//
//     public Task SkipNext()
//     {
//         var command = new SkipNextCommand();
//         return EnqueueCommandAsync(command);
//     }
//
//     public Task SkipPrevious()
//     {
//         var command = new SkipPrevCommand();
//         return EnqueueCommandAsync(command);
//     }
//
//     public Task AddToQueue(WaveePlayerMediaItem mediaItem)
//     {
//         var command = new AddToQueueCommand(mediaItem);
//         return EnqueueCommandAsync(command);
//     }
//
//     public Task PlayMediaItemAsync(
//         WaveePlayerMediaItem mediaItem,
//         TimeSpan startFrom,
//         WaveePlayerPlaybackContext? context = null,
//         bool? overrideShuffling = null,
//         RepeatMode? overrideRepeatMode = null)
//     {
//         var cmdContext = context ?? Context!;
//         var command = new PlayTrackCommand(mediaItem, cmdContext, startFrom);
//         return EnqueueCommandAsync(command);
//     }
//
//     public Task PlayMediaItemAsync(WaveePlayerPlaybackContext context, int pageIndex, int trackIndex)
//     {
//         var command = new PlayTrackAtIndexCommand(context, pageIndex, trackIndex);
//         return EnqueueCommandAsync(command);
//     }
//
//     public Task SetShuffle(bool value)
//     {
//         var command = new SetShuffleCommand(value);
//         return EnqueueCommandAsync(command);
//     }
//
//     public Task SetRepeatMode(RepeatMode mode)
//     {
//         var command = new SetRepeatModeCommand(mode);
//         return EnqueueCommandAsync(command);
//     }
//
//     public Task<List<WaveePlayerMediaItem>> GetUpcomingTracksAsync(int count, CancellationToken cancellationToken)
//     {
//         var result = _trackQueue.GetFutureItems().Take(count).ToList();
//         return Task.FromResult(result);
//     }
//
//     public Task<List<WaveePlayerMediaItem>> GetPreviousTracksInCOntextAsync(int count,
//         CancellationToken cancellationToken)
//     {
//         var result = _trackQueue.GetPreviousItems().Take(count).ToList();
//         return Task.FromResult(result);
//     }
//
//     public Task<List<WaveePlayerMediaItem>> GetPreviousTracksInContextAsync(int count,
//         CancellationToken cancellationToken)
//     {
//         var result = _trackQueue.GetPreviousItems().Take(count).ToList();
//         return Task.FromResult(result);
//     }
//
//     private async Task EnqueueCommandAsync(PlayerCommand command)
//     {
//         await _commandChannel.Writer.WriteAsync(command, _cancellationTokenSource.Token);
//     }
//
//     private async Task<WaveePlayerMediaItem?> RequestNextTrackAsync(CancellationToken cancellationToken)
//     {
//         if (_trackQueue == null)
//         {
//             _logger.LogWarning("Track queue is not initialized.");
//             return null;
//         }
//
//         var nextItem = await _trackQueue.NextItem(true, cancellationToken);
//         if (nextItem == null)
//         {
//             _logger.LogWarning("No more tracks to play.");
//             throw new WaveeKnownPlaybackException(WaveeKnownPlaybackError.NoMoreTracks);
//         }
//
//         _logger.LogInformation("Playing next track: {TrackName}", nextItem);
//         return nextItem;
//     }
//
//     public async ValueTask DisposeAsync()
//     {
//         _cancellationTokenSource.Cancel();
//         _commandChannel.Writer.Complete();
//
//         await Task.WhenAll(
//             PlaybackLoopAsync(CancellationToken.None),
//             CommandLoopAsync(CancellationToken.None));
//
//         //  _audioOutput.Dispose();
//         _stateSubject.Dispose();
//         _cancellationTokenSource.Dispose();
//         // _lock.Dispose();
//     }
//
//     // Abstract Command Class
//     private abstract record PlayerCommand : IDisposable
//     {
//         private readonly TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
//         public Task Completed => _tcs.Task;
//
//         public void Error(Exception exception) => _tcs.TrySetException(exception);
//         public void Complete() => _tcs.TrySetResult();
//
//         public virtual void Dispose()
//         {
//         }
//     }
//
//     // Concrete Command Records
//     private sealed record PlayTrackAtIndexCommand(
//         WaveePlayerPlaybackContext Context,
//         int PageIndex,
//         int TrackIndex) : PlayerCommand;
//
//     private sealed record PlayTrackCommand(
//         WaveePlayerMediaItem MediaItem,
//         WaveePlayerPlaybackContext Context,
//         TimeSpan StartFrom) : PlayerCommand;
//
//     private sealed record AddToQueueCommand(
//         WaveePlayerMediaItem MediaItem) : PlayerCommand;
//
//     private sealed record SkipPrevCommand() : PlayerCommand;
//
//     private sealed record SkipNextCommand() : PlayerCommand;
//
//     private sealed record PauseCommand() : PlayerCommand;
//
//     private sealed record ResumeCommand() : PlayerCommand;
//
//     private sealed record SeekCommand(
//         TimeSpan Position) : PlayerCommand;
//
//     private sealed record SetShuffleCommand(
//         bool Value) : PlayerCommand;
//
//     private sealed record SetRepeatModeCommand(
//         RepeatMode Mode) : PlayerCommand;
// }
//
// // Extension methods for sample providers
// internal static class SampleProviderExtensions
// {
//     public static ISampleProvider WithVolume(this ISampleProvider source, float volume)
//     {
//         return new VolumeSampleProvider(source) { Volume = volume };
//     }
// }