using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using NeoSmart.AsyncLock;
using Wavee.Config;
using Wavee.Enums;
using Wavee.Interfaces;
using Wavee.Models.Common;
using Wavee.Playback.Contexts;
using Wavee.Playback.Player;
using Wavee.Services.Playback;

namespace ConsoleApp1.Player;

public sealed class WaveePlayer : IWaveePlayer
{
    private readonly AsyncLock _commandingLock = new();
    private IAudioOutput _output;
    private DateTimeOffset? _startPlaybackTime;
    private readonly ILogger<IWaveePlayer> _logger;
    private readonly BehaviorSubject<SpotifyLocalPlaybackState?> _stateSubject = new(null);
    private readonly WaveePlayerTrackQueue _trackQueue = new();
    private readonly Dictionary<SpotifyId, WaveePlaybackStream> _prefetchedStreams = new();

    public WaveePlayer(ILogger<IWaveePlayer> logger)
    {
        _logger = logger;
    }

    public async Task Initialize()
    {
        _output = await Task.Run(() => new NAudioOutput(new SpotifyConfig()));
        _output.MediaEnded += async (sender, args) =>
        {
            if (args.Reason is not WaveePlaybackStreamEndedReasonType.ManualStop)
            {
                await SkipNext();
            }
        };
        _output.PrefetchRequested += async (sender, args) =>
        {
            var nextTrack = await _trackQueue.PeekNext();
            if (nextTrack == null)
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                if (!_prefetchedStreams.TryGetValue(nextTrack.Id.Value, out var stream) || !stream.IsAlive)
                {
                    var playbackStream = new WaveePlaybackStream(nextTrack, RequestAudioStreamForTrack);
                    await playbackStream.Open(CancellationToken.None);
                    _prefetchedStreams[nextTrack.Id.Value] = playbackStream;
                }
            });
        };
    }

    public SpotifyConfig Config { get; set; } = null!;
    public ITimeProvider TimeProvider { get; set; } = null!;

    public IObservable<SpotifyLocalPlaybackState?> State =>
        _stateSubject.AsObservable().Where(x => x != null).Throttle(TimeSpan.FromMilliseconds(50));

    public float Volume => _output.Volume;
    public WaveePlayerPlaybackContext? Context => _trackQueue.Context;
    public RequestAudioStreamForTrackAsync? RequestAudioStreamForTrack { get; set; }
    public TimeSpan Position => _output.Position;

    public Task<List<WaveePlayerMediaItem>> GetUpcomingTracksAsync(int count, CancellationToken cancellationToken)
    {
        return Task.FromResult(new List<WaveePlayerMediaItem>());
    }

    public Task<List<WaveePlayerMediaItem>> GetPreviousTracksInCOntextAsync(int count,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new List<WaveePlayerMediaItem>());
    }

    public Task Stop()
    {
        _output.Stop();
        UpdatePlaybackState();
        return Task.CompletedTask;
    }

    public Task Pause()
    {
        _output.Pause();
        UpdatePlaybackState();
        return Task.CompletedTask;
    }

    public Task Resume()
    {
        _output.Resume();
        UpdatePlaybackState();
        return Task.CompletedTask;
    }

    public Task Seek(TimeSpan to)
    {
        _output.Seek(to);
        UpdatePlaybackState();
        return Task.CompletedTask;
    }

    public Task SetVolume(float volume)
    {
        _output.SetVolume(volume);
        UpdatePlaybackState();
        return Task.CompletedTask;
    }

    public async Task SkipNext()
    {
        using (await _commandingLock.LockAsync())
        {
            var nextTrack = await _trackQueue.Next();
            if (nextTrack == null)
            {
                _output.Stop();
                _latestState = null;
                _stateSubject.OnNext(null);
            }
            else
            {
                await PlayMediaItemAsync(Context!, nextTrack, TimeSpan.Zero);
            }
        }
    }

    public async Task SkipPrevious()
    {
        using (await _commandingLock.LockAsync())
        {
            var prevTrack = await _trackQueue.Previous();
            if (prevTrack == null)
            {
                _trackQueue.Reset();
                prevTrack = await _trackQueue.Next();
            }

            if (prevTrack is null)
            {
                _logger.LogWarning("No previous track found.");
                return;
            }

            await PlayMediaItemAsync(Context!, prevTrack, TimeSpan.Zero);
        }
    }

    public Task AddToQueue(WaveePlayerMediaItem mediaItem)
    {
        _trackQueue.Enqueue(mediaItem);
        UpdatePlaybackState();
        return Task.CompletedTask;
    }

    public async Task PlayMediaItemAsync(
        WaveePlayerPlaybackContext context,
        WaveePlayerMediaItem mediaItem,
        TimeSpan startFrom,
        bool? overrideShuffling = null, RepeatMode? overrideRepeatMode = null)
    {
        using (await _commandingLock.LockAsync())
        {
            _startPlaybackTime ??= await TimeProvider.CurrentTime();
            _startPlaybackTime = _startPlaybackTime.Value.AddMilliseconds(500);
            if (!mediaItem.Id.HasValue && !string.IsNullOrEmpty(mediaItem.Uid))
            {
                await context.InitializePages();
                mediaItem.Id = await context.GetTrackId(mediaItem.Uid);
            }

            _latestState = new SpotifyLocalPlaybackState(_startPlaybackTime, Config.Playback.DeviceId,
                Config.Playback.DeviceName,
                false,
                true,
                mediaItem.Id.Value,
                mediaItem.Uid,
                positionSinceSw: startFrom,
                stopwatch: Stopwatch.StartNew(),
                totalDuration: mediaItem.Duration ?? TimeSpan.Zero,
                repeatState: RepeatMode.Off,
                isShuffling: false,
                contextUrl: "context://" + context.Id,
                contextUri: context.Id,
                currentTrack: null,
                currentTrackMetadata: new Dictionary<string, string>()
            );
            UpdatePlaybackState();
            // To allow for superfast playback, we will always just play the media item immediately, and not wait for the context to be ready
            Task.Run(async () => await _trackQueue.FromContext(context, mediaItem));

            if (!_prefetchedStreams.TryGetValue(mediaItem.Id.Value, out var stream) || !stream.IsAlive)
            {
                var playbackStream = new WaveePlaybackStream(mediaItem, RequestAudioStreamForTrack);
                stream = playbackStream;
                _prefetchedStreams[mediaItem.Id.Value] = playbackStream;
            }

            await Task.Run(async () =>
            {
                await _output.Play(stream!, CancellationToken.None);
                _output.Seek(startFrom);
                UpdatePlaybackState();
            });
        }
    }

    public Task PlayMediaItemAsync(WaveePlayerPlaybackContext context, int pageIndex, int trackIndex)
    {
        throw new NotImplementedException();
    }

    public Task SetShuffle(bool value)
    {
        _trackQueue.Shuffle(value);
        UpdatePlaybackState();
        return Task.CompletedTask;
    }

    public Task SetRepeatMode(RepeatMode mode)
    {
        _trackQueue.SetRepeatMode(mode);
        UpdatePlaybackState();
        return Task.CompletedTask;
    }

    private void UpdatePlaybackState()
    {
        _latestState = _latestState.WithNewPosition(_output.Position);
        _latestState = _latestState.WithTrack(_output.CurrentPlaybackStream?.SpotifyItem);
        _latestState = _latestState.WithIsPaused(_output.IsPaused);
        _latestState = _latestState.WithIsShuffling(_trackQueue.Shuffling);
        _latestState = _latestState.WithRepeatMode(_trackQueue.RepeatMode);
        _stateSubject.OnNext(_latestState);
    }

    private SpotifyLocalPlaybackState? _latestState;
}