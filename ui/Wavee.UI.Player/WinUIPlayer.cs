using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Windows.Media.Audio;
using Windows.Media.Core;
using Windows.Media.Render;
using Microsoft.Extensions.Logging;
using NeoSmart.AsyncLock;
using Wavee.Config;
using Wavee.Enums;
using Wavee.Interfaces;
using Wavee.Models.Common;
using Wavee.Playback.Contexts;
using Wavee.Playback.Player;
using Wavee.Services.Playback;

namespace Wavee.UI.Player;

public class WinUIPlayer : IWaveePlayer, IDisposable
{
    private AudioGraph _audioGraph;
    private AudioDeviceOutputNode _outputNode;
    private AudioSubmixNode _submixNode;
    private readonly AsyncLock _queueLock = new AsyncLock();
    private readonly Queue<WaveePlayerMediaItem> _playbackQueue = new Queue<WaveePlayerMediaItem>();
    private WaveePlayerMediaItem _currentMediaItem;

    private readonly BehaviorSubject<SpotifyLocalPlaybackState> _state =
        new BehaviorSubject<SpotifyLocalPlaybackState>(null);

    private DateTimeOffset? _playingSince;
    private readonly AsyncLock _stateLock = new AsyncLock();

    private MediaSourceAudioInputNode? _currentInputNode;
    private readonly Dictionary<SpotifyId, MediaSourceAudioInputNode> _inputNodes = new();
    private MediaSourceAudioInputNode? _nextInputNode;
    private WaveePlayerMediaItem? _nextMediaItem;
    private readonly LinkedList<SpotifyId> _nodeOrder = new LinkedList<SpotifyId>();

    private readonly Dictionary<SpotifyId, LinkedListNode<SpotifyId>> _nodeOrderMap =
        new Dictionary<SpotifyId, LinkedListNode<SpotifyId>>();

    private int MaxActiveNodes => Config.Playback.MaxActiveNodes;

    public WinUIPlayer(ILogger logger)
    {
        _logger = logger;
        InitializeAudioGraphAsync().Wait();
    }

    // Properties
    public Task Initialize()
    {
        throw new NotImplementedException();
    }

    public SpotifyConfig Config { get; set; }
    public ITimeProvider TimeProvider { get; set; }
    public IObservable<SpotifyLocalPlaybackState?> State => _state.AsObservable().Where(x => x != null);

    public float Volume
    {
        get => (float)_submixNode.OutgoingGain;
        set => _submixNode.OutgoingGain = value;
    }

    public WaveePlayerPlaybackContext? Context { get; private set; }
    private int _currentPageIndex = -1;
    private int _currentTrackIndex = -1;

    public RequestAudioStreamForTrackAsync? RequestAudioStreamForTrack { get; set; }

    public TimeSpan Position
    {
        get
        {
            if (_currentInputNode != null)
            {
                return _currentInputNode.Position;
            }

            return TimeSpan.Zero;
        }
    }

    private SpotifyLocalPlaybackState _previousState;
    private readonly ILogger _logger;

    #region AudioGraph Initialization

    private async Task InitializeAudioGraphAsync()
    {
        var settings = new AudioGraphSettings(AudioRenderCategory.Media)
        {
            QuantumSizeSelectionMode = QuantumSizeSelectionMode.LowestLatency
        };

        var result = await AudioGraph.CreateAsync(settings);
        if (result.Status != AudioGraphCreationStatus.Success)
        {
            throw new Exception("AudioGraph creation failed: " + result.Status.ToString());
        }

        _audioGraph = result.Graph;

        // Create the output node
        var outputResult = await _audioGraph.CreateDeviceOutputNodeAsync();
        if (outputResult.Status != AudioDeviceNodeCreationStatus.Success)
        {
            throw new Exception("Audio Device Output Node creation failed: " + outputResult.Status.ToString());
        }

        _outputNode = outputResult.DeviceOutputNode;

        // Create a submix node for mixing multiple inputs
        var submixResult = _audioGraph.CreateSubmixNode();
        _submixNode = submixResult;

        // Connect the submix to the output
        _submixNode.AddOutgoingConnection(_outputNode);

        // Start the graph
        _audioGraph.Start();

        // Subscribe to QuantumStarted for state updates
        _audioGraph.QuantumStarted += OnQuantumStarted;
    }

    private void OnQuantumStarted(AudioGraph sender, object args)
    {
        // UpdatePlaybackState();
    }

    #endregion

    #region Playback Controls

    public Task Pause()
    {
        _audioGraph.Stop();
        _currentInputNode?.Stop();
        UpdatePlaybackState(isPaused: true);
        return Task.CompletedTask;
    }

    public Task Resume()
    {
        _audioGraph.Start();
        _currentInputNode?.Start();
        UpdatePlaybackState(isPaused: false);
        return Task.CompletedTask;
    }

    public Task<List<WaveePlayerMediaItem>> GetPreviousTracksInCOntextAsync(int count,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new List<WaveePlayerMediaItem>());
    }

    public Task Stop()
    {
        _audioGraph.Stop();
        _currentInputNode?.Stop();
        ClearQueue();
        UpdatePlaybackState(isStopped: true);
        _playingSince = null;
        return Task.CompletedTask;
    }

    public Task Seek(TimeSpan to)
    {
        if (_currentInputNode != null)
        {
            _currentInputNode.Seek(to);
            UpdatePlaybackState();
        }

        return Task.CompletedTask;
    }

    public Task SetVolume(float volume)
    {
        Volume = volume;
        return Task.CompletedTask;
    }

    public async Task SkipNext()
    {
        await EnqueueNextTrack();
    }

    public Task SkipPrevious()
    {
        // Implement skip previous logic if needed
        return Task.CompletedTask;
    }

    #endregion

    #region Queue Management

    public async Task AddToQueue(WaveePlayerMediaItem mediaItem)
    {
        using (await _queueLock.LockAsync())
        {
            int queueId = 0;
            foreach (var item in _playbackQueue)
            {
                queueId = Math.Max(queueId, item.QueueId ?? 0);
            }

            mediaItem.QueueId = queueId + 1;
            _playbackQueue.Enqueue(mediaItem);
        }

        UpdatePlaybackState();
    }


    private async Task PlayMediaItemInternalAsync(WaveePlayerMediaItem mediaItem, bool prefetchNext)
    {
        using (await _stateLock.LockAsync())
        {
            _currentInputNode?.Stop();
            _currentMediaItem = mediaItem;

            // Use pre-cached node if available
            if (_inputNodes.TryGetValue(mediaItem.Id.Value, out var inputNode))
            {
                _currentMediaItem = mediaItem;
                _currentInputNode = inputNode;
                inputNode.Seek(TimeSpan.Zero);
                _currentInputNode.Start();
                UpdatePlaybackState();

                // Update node tracking to mark it as recently used
                await UpdateNodeUsageAsync(mediaItem.Id.Value);

                // Pre-cache the next track
                if (prefetchNext)
                {
                    _ = Task.Run(async () => await PreCacheNextTrackAsync());
                }
                return;
            }

            var sw = Stopwatch.StartNew();

            var fileInputNode = await CreateNode(mediaItem);
            if (fileInputNode == null)
            {
                _logger.LogError("Failed to create input node for {Track}", mediaItem.Id);
                await SkipNext();
                return;
            }

            _inputNodes[mediaItem.Id.Value] = fileInputNode;
            _currentInputNode = fileInputNode;
            _audioGraph.Start();
            fileInputNode.Start();
            _playingSince ??= TimeProvider?.CurrentTime().Result ?? DateTimeOffset.Now;

            _logger.LogInformation("Playing {Track} in {Duration} ms", mediaItem.Id, sw.ElapsedMilliseconds);
            UpdatePlaybackState(isPaused: false);

            if (prefetchNext)
            {
                // Pre-cache the next track
                _ = Task.Run(async () => await PreCacheNextTrackAsync());
            }
        }
    }

    private async Task<MediaSourceAudioInputNode?> CreateNode(WaveePlayerMediaItem item)
    {
        // request the audio stream
        var lazy = new LazyStreamReference(RequestAudioStreamForTrack, item);
        var mediaSource = MediaSource.CreateFromStreamReference(lazy, "audio/ogg");
        mediaSource.CustomProperties["stream"] = lazy;
        var fileInputResult = await _audioGraph.CreateMediaSourceAudioInputNodeAsync(mediaSource);
        if (fileInputResult.Status != MediaSourceAudioInputNodeCreationStatus.Success)
        {
            Console.WriteLine("Failed to create AudioFileInputNode: " + fileInputResult.Status.ToString());
            return null;
        }

        var fileInputNode = fileInputResult.Node;
        fileInputNode.AddOutgoingConnection(_submixNode);
        fileInputNode.MediaSourceCompleted += async (sender, args) => { await SkipNext(); };
        fileInputNode.Stop();

        await AddNodeToTrackingAsync(item.Id.Value);

        return fileInputNode;
    }

    private async Task UpdateNodeUsageAsync(SpotifyId id)
    {
        if (_nodeOrderMap.TryGetValue(id, out var node))
        {
            // Move the node to the end of the LinkedList
            _nodeOrder.Remove(node);
            _nodeOrder.AddLast(node);
        }
    }


    private async Task AddNodeToTrackingAsync(SpotifyId id)
    {
        using (await _stateLock.LockAsync())
        {
            // Add the new node to the end of the LinkedList
            var node = _nodeOrder.AddLast(id);
            _nodeOrderMap[id] = node;

            // Check if we exceed the maximum number of active nodes
            if (_nodeOrder.Count > MaxActiveNodes)
            {
                // Remove the oldest node (first in the LinkedList)
                var oldestId = _nodeOrder.First.Value;
                var oldestNode = _nodeOrder.First;

                // Dispose the corresponding MediaSourceAudioInputNode
                if (_inputNodes.TryGetValue(oldestId, out var oldestInputNode))
                {
                    oldestInputNode.Stop();
                    oldestInputNode.Dispose();
                    _inputNodes.Remove(oldestId);
                    _logger.LogInformation("Disposed oldest input node for Track ID: {TrackId}", oldestId);
                }

                // Remove from tracking structures
                _nodeOrder.RemoveFirst();
                _nodeOrderMap.Remove(oldestId);
            }
        }
    }

    private async Task PreCacheNextTrackAsync()
    {
        using (await _stateLock.LockAsync())
        {
            try
            {
                var nextTrack = await GetNextTrackAsync();
                if (nextTrack == null)
                {
                    _logger.LogInformation("No next track to pre-cache.");
                    return;
                }

                if (_inputNodes.ContainsKey(nextTrack.Id.Value))
                {
                    _nextInputNode = _inputNodes[nextTrack.Id.Value];
                    _nextMediaItem = nextTrack;
                    return;
                }

                var fileInputNode = await CreateNode(nextTrack);
                if (fileInputNode == null)
                {
                    _logger.LogError("Failed to pre-cache input node for {Track}", nextTrack.Id);
                    return;
                }

                _inputNodes[nextTrack.Id.Value] = fileInputNode;
                _nextInputNode = fileInputNode;
                _nextMediaItem = nextTrack;

                _logger.LogInformation("Pre-cached next track {Track}", nextTrack.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while pre-caching the next track.");
            }
        }
    }

    private async Task<WaveePlayerMediaItem?> GetNextTrackAsync()
    {
        using (await _stateLock.LockAsync())
        {
            if (_playbackQueue.Count > 0)
            {
                return _playbackQueue.Dequeue();
            }


            var pages = await Context.InitializePages();

            // Locate the current track's position
            for (var i = 0; i < pages.Count; i++)
            {
                var page = pages.ElementAt(i);
                int trackIndex = 0;
                while (true)
                {
                    var item = await page.GetItemAt(trackIndex, CancellationToken.None);
                    if (item == null)
                    {
                        break;
                    }

                    if (item.Is(_currentMediaItem))
                    {
                        _currentPageIndex = i;
                        _currentTrackIndex = trackIndex;
                        break;
                    }

                    trackIndex++;
                }

                if (_currentPageIndex != -1)
                {
                    break;
                }
            }

            if (_currentPageIndex == -1)
            {
                return null;
            }

            var currentPage = pages.ElementAt(_currentPageIndex);
            var nextTrackIndex = _currentTrackIndex + 1;
            var actualNextTrack = await currentPage.GetItemAt(nextTrackIndex, CancellationToken.None);
            if (actualNextTrack == null)
            {
                // Try to get the next page
                var nextPage = await Context.GetPage(_currentPageIndex + 1, CancellationToken.None);
                if (nextPage == null)
                {
                    return null;
                }

                var nextTrack = await nextPage.GetItemAt(0, CancellationToken.None);
                if (nextTrack == null)
                {
                    return null;
                }

                _currentPageIndex++;
                _currentTrackIndex = 0;
                return nextTrack;
            }
            else
            {
                _currentTrackIndex++;
                return actualNextTrack;
            }
        }
    }

    private async Task EnqueueNextTrack()
    {
        using (await _stateLock.LockAsync())
        {
            if (_playbackQueue.TryDequeue(out var q))
            {
                await PlayMediaItemInternalAsync(q, false);
                return;
            }

            if (_nextInputNode != null && _nextMediaItem != null)
            {
                _currentInputNode?.Stop();

                _currentInputNode = _nextInputNode;
                _currentMediaItem = _nextMediaItem;
                _currentInputNode.Seek(TimeSpan.Zero);
                _currentInputNode.Start();
                UpdatePlaybackState();

                // Start pre-caching the following track
                _ = Task.Run(async () => await PreCacheNextTrackAsync());
            }
            else
            {
                var nextTrack = await GetNextTrackAsync();
                if (nextTrack != null)
                {
                    await PlayMediaItemInternalAsync(nextTrack, true);
                }
            }
        }
    }


    private void ClearQueue()
    {
    }

    #endregion

    #region Playback State Management

    private void UpdatePlaybackState(bool? isPaused = null, bool? isStopped = null)
    {
        if (_currentMediaItem == null)
        {
            _state.OnNext(null);
            return;
        }

        var position = Position;
        var streamRef = _currentInputNode.MediaSource.CustomProperties["stream"] as LazyStreamReference;
        var stream = streamRef.Stream;
        _previousState = new SpotifyLocalPlaybackState(
            playingSince: _playingSince,
            deviceId: Config.Playback.DeviceId,
            deviceName: Config.Playback.DeviceName,
            isPaused: isPaused ?? _previousState?.IsPaused ?? false,
            isBuffering: false,
            trackId: _currentMediaItem.Id.Value,
            trackUid: _currentMediaItem.Uid,
            positionSinceSw: position,
            stopwatch: isPaused is true ? new Stopwatch() : Stopwatch.StartNew(),
            totalDuration: stream?.Track?.Duration ?? TimeSpan.Zero,
            repeatState: RepeatMode.Off, // Update based on your logic
            isShuffling: false, // Update based on your logic
            contextUrl: "context://" + Context?.Id,
            contextUri: Context?.Id ?? "",
            currentTrack: stream?.Track,
            currentTrackMetadata: new Dictionary<string, string>() // Populate as needed
        );

        _state.OnNext(_previousState);
    }

    #endregion

    #region Media Item Playback

    public async Task PlayMediaItemAsync(WaveePlayerMediaItem mediaItem,
        TimeSpan startFrom,
        WaveePlayerPlaybackContext? context = null,
        bool? overrideShuffling = null,
        RepeatMode? overrideRepeatMode = null)
    {
        Context = context;
        _currentMediaItem = mediaItem;
        _playingSince ??= await TimeProvider.CurrentTime();

        var sw = Stopwatch.StartNew();
        await PlayMediaItemInternalAsync(mediaItem, true);
        sw.Stop();
        // Optionally seek to the specified start time
        if (startFrom > TimeSpan.Zero)
        {
            await Seek(startFrom);
        }
    }

    public Task PlayMediaItemAsync(WaveePlayerPlaybackContext context, int pageIndex, int trackIndex)
    {
        // Implement logic to play a media item based on context, pageIndex, and trackIndex
        // This might involve fetching the specific track from a playlist or context
        return Task.CompletedTask;
    }

    public Task SetShuffle(bool value)
    {
        throw new NotImplementedException();
    }

    public Task SetRepeatMode(RepeatMode mode)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region Additional Methods

    public async Task<List<WaveePlayerMediaItem>> GetUpcomingTracksAsync(int count, CancellationToken cancellationToken)
    {
        using (_queueLock.LockAsync().Result)
        {
            var list = new List<WaveePlayerMediaItem>();
            var enumerator = _playbackQueue.GetEnumerator();
            while (enumerator.MoveNext() && list.Count < count)
            {
                list.Add(enumerator.Current);
            }

            // now add the next tracks from regular playback
            if (Context is not null)
            {
                var currentPage = await Context.GetPage(_currentPageIndex, CancellationToken.None);
                if (currentPage is not null && currentPage.PeekTracks(out var nextTracks))
                {
                    list.AddRange(nextTracks.Skip(_currentTrackIndex));
                }
            }

            return list.Take(count).ToList();
        }
    }

    public Task<List<WaveePlayerMediaItem>> GetPreviousTracksInContextAsync(int count,
        CancellationToken cancellationToken)
    {
        // Implement logic to retrieve previous tracks within the current context
        return Task.FromResult(new List<WaveePlayerMediaItem>());
    }

    #endregion

    #region Dispose Pattern

    public void Dispose()
    {
        ClearQueue();
        _outputNode?.Dispose();
        _submixNode?.Dispose();
        _audioGraph?.Dispose();
        _state?.Dispose();
        // Dispose other managed resources if any
    }

    #endregion
}