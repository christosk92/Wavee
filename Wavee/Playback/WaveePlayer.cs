using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Wavee.Helper;
using Wavee.Playback.Converters;
using Wavee.Playback.Decoder;
using Wavee.Playback.Factories;
using Wavee.Playback.Item;
using Wavee.Playback.Normalisation;
using Wavee.Playback.Packets;
using Wavee.Playback.Volume;

namespace Wavee.Playback;

public sealed class WaveePlayer
{
    private bool _autoNormaliseAsAlbum;
    private readonly Channel<IWaveePlayerCommand> _commands;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ILogger<WaveePlayer>? _logger;


    private SinkStatus _sinkStatus = SinkStatus.Closed;
    private readonly WaveePlayerConfig _config;
    private readonly IAudioSink _sink;
    private readonly IAudioConverter _converter;
    private readonly IVolumeGetter _volumeGetter;
    private readonly ITrackLoader _trackLoader;
    private SinkEventDelegate _sinkEventCallback;

    private ConcurrentDictionary<int, Task> LoadHandles
    {
        get;
    } = new ConcurrentDictionary<int, Task>();

    public WaveePlayer(
        WaveePlayerConfig config,
        ITrackLoader trackLoader,
        IAudioSink sink,
        IVolumeGetter? volumeGetter = null,
        ILogger<WaveePlayer>? logger = null)
    {
        _config = config;
        _sink = sink;
        _converter = new StdAudioConverter();
        _volumeGetter = volumeGetter ?? new SoftVolume(1.0);
        _logger = logger;
        _trackLoader = trackLoader;

        Task.Factory.StartNew(Poll, TaskCreationOptions.LongRunning);
        _cancellationTokenSource = new CancellationTokenSource();
        _commands = Channel.CreateUnbounded<IWaveePlayerCommand>();
    }


    public event EventHandler<IWaveePlayerEvent> Events;

    public IWaveePlayerState State
    {
        get;
        private set;
    }

    public string PlayTrack(string trackId, bool startPlayback, double positionMs)
    {
        var playRequestId = GeneratePlaybackId();
        _commands.Writer.TryWrite(new LoadTrackCommand(trackId, playRequestId, startPlayback, positionMs));
        return playRequestId;
    }

    private async Task Poll()
    {
        AsyncManualResetEvent? waitForPlayback = new(false);
        var all_futures_completed_or_not_ready = true;
        // This Task.Run is needed because the PlayerInternal contains blocking code
        // It must be run on its own thread
        await Task.Factory.StartNew(async () =>
        {
            // Process commands that were sent to us
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                var cmd = await _commands.Reader.ReadAsync(_cancellationTokenSource.Token);
                try
                {
                    HandleCommand(cmd);
                    waitForPlayback.Set();
                }
                catch (Exception? e)
                {
                    _logger.LogError(e, "Error handling command: {cmd}", cmd);
                }
            }
        }, TaskCreationOptions.LongRunning);

        while (!_cancellationTokenSource.IsCancellationRequested)
        {
            bool allFuturesCompletedOrNotReady = true;

            // Handle loading of a new track to play
            if (State is WaveeLoadingState loadingState)
            {
                // The loader may be terminated if we are trying to load the same track
                // as before, and that track failed to open before.
                if (loadingState.Loader is { IsCompleted: false, IsFaulted: false, IsCanceled: false })
                {
                    try
                    {
                        var loadedTrack = await loadingState.Loader;

                        StartPlayback(loadingState.TrackId, loadingState.PlayRequestId, loadedTrack,
                            loadingState.StartPlayback);

                        if (State is WaveeLoadingState)
                        {
                            _logger.LogError("The state wasn't changed by StartPlayback()");
                            Environment.Exit(1);
                        }
                    }
                    catch (Exception? e)
                    {
                        _logger.LogError(e, "Skipping to next track, unable to load track: {trackId}",
                            loadingState.TrackId);
                        SendEvent(new UnavailableEvent
                            { TrackId = loadingState.TrackId, PlayRequestId = loadingState.PlayRequestId });
                    }
                }
            }

            //TODO: Handle preload requests

            if (State.IsPlaying())
            {
                if (State is WaveePlayingState p)
                {
                    EnsureSinkRunning(p.Format.Channels, p.Format.SampleRate);
                    try
                    {
                        var nextPacket = p.Decoder.NextPacket();
                        if (nextPacket is var (packetPosition, packet))
                        {
                            var newStreamPositionMs = packetPosition.PositionMs;
                            var pStreamPositionMs = p.StreamPositionMs;
                            var expectedPositionMs = Interlocked.Exchange(ref pStreamPositionMs,
                                newStreamPositionMs);
                            p.StreamPositionMs = pStreamPositionMs;

                            switch (packet)
                            {
                                case SamplesPacket samples:
                                    var newStreamPosition = TimeSpan.FromMilliseconds(newStreamPositionMs);

                                    var now = DateTimeOffset.Now;

                                    // Only notify if we're skipped some packets *or* we are behind.
                                    // If we're ahead it's probably due to a buffer of the backend
                                    // and we're actually in time.
                                    bool notifyAboutPosition = false;
                                    if (p.ReportedNominalStartTime is null)
                                    {
                                        notifyAboutPosition = true;
                                    }
                                    else
                                    {
                                        bool notify = false;

                                        if (packetPosition.Skipped)
                                        {
                                            TimeSpan? ahead = newStreamPosition -
                                                              TimeSpan.FromMilliseconds(expectedPositionMs);
                                            if (ahead.HasValue && ahead.Value >= TimeSpan.FromSeconds(1))
                                            {
                                                notify = true;
                                            }
                                        }

                                        TimeSpan? lag = DateTime.UtcNow - p.ReportedNominalStartTime.Value;
                                        if (lag.HasValue)
                                        {
                                            TimeSpan? lagDifference = lag.Value - newStreamPosition;
                                            if (lagDifference.HasValue &&
                                                lagDifference.Value >= TimeSpan.FromSeconds(1))
                                            {
                                                notify = true;
                                            }
                                        }

                                        notifyAboutPosition = notify;
                                    }

                                    if (notifyAboutPosition)
                                    {
                                        p.ReportedNominalStartTime
                                            = DateTime.UtcNow - TimeSpan.FromMilliseconds(newStreamPositionMs);

                                        SendEvent(new PositionCorrectionEvent(
                                            PlayRequestId: p.PlayRequestId,
                                            TrackId: p.TrackId,
                                            PositionMs: newStreamPositionMs
                                        ));
                                    }

                                    break;
                            }
                        }

                        HandlePacket(nextPacket, p.NormalisationFactor);
                    }
                    catch (Exception? x)
                    {
                        _logger.LogError(x, "Skipping to next track, unable to decode track: {trackId}", p.TrackId);
                        SendEvent(new EndOfTrackEvent { TrackId = p.TrackId, PlayRequestId = p.PlayRequestId });
                    }
                }
            }

            if (!State.IsPlaying() && allFuturesCompletedOrNotReady)
            {
                waitForPlayback.Reset();
                await waitForPlayback.WaitAsync(_cancellationTokenSource.Token);
            }
        }
    }

    private void StartPlayback(string trackId,
        string playRequestId,
        PlayerLoadedTrackData loadedTrack,
        bool startPlayback)
    {
        var audioItem = (PlaybackItem)loadedTrack.AudioItem.Copy();

        // self.send_event(PlayerEvent::TrackChanged { audio_item });
        SendEvent(new TrackChangedEvent
        {
            Item = audioItem
        });

        var position_ms = loadedTrack.StreamPositionMs;

        var config = _config with
        {
            NormalisationType = _autoNormaliseAsAlbum ? WaveeNormalisationType.Album : WaveeNormalisationType.Track
        };

        /*
           let normalisation_factor =
            NormalisationData::get_factor(&config, loaded_track.normalisation_data);
            */

        var normalisationFactor = NormalisationData.GetFactor(config, loadedTrack.NormalisationData, _logger);

        if (startPlayback)
        {
            EnsureSinkRunning(loadedTrack.Format.Channels, loadedTrack.Format.SampleRate);
            SendEvent(new PlayingPlayerEvent(
                TrackId: trackId,
                PlayRequestId: playRequestId,
                PositionMs: position_ms
            ));

            State = new WaveePlayingState(
                TrackId: trackId,
                PlayRequestId: playRequestId,
                Decoder: new AudioDecoder(loadedTrack.Format),
                Format: loadedTrack.Format,
                AudioItem: audioItem,
                NormalisationData: loadedTrack.NormalisationData,
                NormalisationFactor: normalisationFactor,
                StreamLoaderController: loadedTrack.StreamLoaderController,
                DurationMs: loadedTrack.AudioItem.Item.Duration,
                BytesPerSecond: loadedTrack.BytesPerSecond,
                StreamPositionMs: loadedTrack.StreamPositionMs,
                ReportedNominalStartTime: DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMicroseconds(position_ms)),
                SuggestedToPreloadNextTrack: false,
                IsExplicit: loadedTrack.IsExplicit
            );
        }
        else
        {
            EnsureSinkStopped(false);

            State = new WaveePausedState(
                TrackId: trackId,
                PlayRequestId: playRequestId,
                Decoder: new AudioDecoder(loadedTrack.Format),
                NormalisationData: loadedTrack.NormalisationData,
                NormalisationFactor: normalisationFactor,
                StreamLoaderController: loadedTrack.StreamLoaderController,
                DurationMs: loadedTrack.AudioItem.Item.Duration,
                BytesPerSecond: loadedTrack.BytesPerSecond,
                StreamPositionMs: loadedTrack.StreamPositionMs,
                SuggestedToPreloadNextTrack: false,
                IsExplicit: loadedTrack.IsExplicit
            );

            SendEvent(new PausedPlayerEvent(
                TrackId: trackId,
                PlayRequestId: playRequestId,
                PositionMs: position_ms
            ));
        }
    }

    private void HandlePause()
    {
    }

    private void HandlePacket((AudioPacketPosition Position, IAudioPacket Packet)? result, double normalisationFactor)
    {
        switch (result)
        {
            case (var position, { } packet):
                if (packet is SamplesPacket samples)
                {
                    if (!samples.IsEmpty)
                    {
                        // Get the volume for the packet.
                        // In the case of hardware volume control this will
                        // always be 1.0 (no change).
                        var volume = _volumeGetter.AttenuationFactor();

                        // For the basic normalisation method, a normalisation factor of 1.0 indicates that
                        // there is nothing to normalise (all samples should pass unaltered). For the
                        // dynamic method, there may still be peaks that we want to shave off.
                        if (!_config.Normalisation && volume < 1.0)
                        {
                            for (var index = 0; index < samples.Samples.Length; index++)
                            {
                                var sample = samples.Samples[index];
                                sample = (byte)(sample * volume);
                                samples.Samples[index] = sample;
                            }
                        }
                        else if (_config.NormalisationMethod == WaveeNormalisationMethod.Basic &&
                                 (normalisationFactor < 1.0 || volume < 1.0))
                        {
                            for (var index = 0; index < samples.Samples.Length; index++)
                            {
                                var sample = samples.Samples[index];
                                sample = (byte)(sample * normalisationFactor * volume);
                                samples.Samples[index] = sample;
                            }
                        }
                        else if (_config.NormalisationMethod == WaveeNormalisationMethod.Dynamic)
                        {
                            double thresholdDb = _config.NormalisationThresholdDbfs;
                            double kneeDb = _config.NormalisationKneeDb;
                            double attackCf = _config.NormalisationAttackCf;
                            double releaseCf = _config.NormalisationReleaseCf;

                            for (var index = 0; index < samples.Samples.Length; index++)
                            {
                                var sample = samples.Samples[index];
                                sample = (byte)(sample * normalisationFactor);
                                samples.Samples[index] = sample;
                            }
                        }
                    }
                }

                try
                {
                    _sink.Write(packet, _converter);
                }
                catch (Exception? x)
                {
                    _logger.LogError(x, "Unable to write packet to sink");
                    HandlePause();
                }

                break;
            case null:
                State = PlayingToEndOfTrack() ?? throw new InvalidOperationException();
                break;
        }
    }

    private void HandleLoadCommand(string loadTrackId, string loadPlayRequestId, bool loadStartPlayback,
        double loadPositionMs)
    {
        if (!_config.Gapless)
        {
            EnsureSinkStopped(loadStartPlayback);
        }

        if (State is InvalidPlayerState inv)
        {
            _logger.LogError("Ignoring load command as player is in invalid state: {inv}", inv);
            return;
        }

        // Now we check at different positions whether we already have a pre-loaded version
        // of this track somewhere. If so, use it and return.

        // Check if there's a matching loaded track in the EndOfTrack player state.
        // This is the case if we're repeating the same track again.
        if (State is EndOfTrackState eot)
        {
            //TODO:
        }

        // Check if we are already playing the track. If so, just do a seek and update our info.
        if (State is WaveePlayingState or WaveePausedState)
        {
            //TODO:
        }

        //TODO: Check if the requested track has been preloaded already. If so use the preloaded data.

        // We need to load the track - either from scratch or by completing a preload.
        // In any case we go into a Loading state to load the track.
        EnsureSinkStopped(loadStartPlayback);

        SendEvent(new LoadingPlayerEvent(loadTrackId, loadPlayRequestId, loadPositionMs));

        Task<PlayerLoadedTrackData>? loader = null;

        // Try to extract a pending loader from the preloading mechanism
        //TODO:


        // If we don't have a loader yet, create one from scratch.
        if (loader is null)
        {
            loader = LoadTrack(loadTrackId, loadPositionMs);
        }

        State = new WaveeLoadingState(
            Loader: loader!,
            loadTrackId,
            loadPlayRequestId,
            loadStartPlayback
        );
    }

    private void HandleCommand(IWaveePlayerCommand item)
    {
        _logger.LogDebug("Handling command {Command}", item);

        switch (item)
        {
            case LoadTrackCommand load:
                HandleLoadCommand(load.TrackId, load.PlayRequestId, load.StartPlayback, load.PositionMs);
                break;
        }
    }


    private void SendEvent(IWaveePlayerEvent playerEvent)
    {
        Events?.Invoke(this, playerEvent);
    }

    private void EnsureSinkRunning(int channels, int sampleRate)
    {
        if (_sinkStatus != SinkStatus.Running)
        {
            _logger.LogDebug("== Starting sink ==");
            _sinkEventCallback?.Invoke(SinkStatus.Running);
            try
            {
                _sink.Start(channels, sampleRate);
                _sinkStatus = SinkStatus.Running;
            }
            catch (Exception? x)
            {
                _logger.LogError(x, "Unable to start sink");
                HandlePause();
            }
        }
    }

    private void EnsureSinkStopped(bool temporarily)
    {
        switch (_sinkStatus)
        {
            case SinkStatus.Running:
                try
                {
                    _sink.Stop();
                    _sinkStatus = temporarily ? SinkStatus.TemporaryStopped : SinkStatus.Closed;
                    _sinkEventCallback?.Invoke(_sinkStatus);
                }
                catch (Exception? x)
                {
                    _logger.LogError(x, "Unable to stop sink");
                    HandlePause();
                }

                break;
            case SinkStatus.Closed:
                // Nothing to do
                break;
            case SinkStatus.TemporaryStopped:
                if (!temporarily)
                {
                    _sinkStatus = SinkStatus.Closed;
                    _sinkEventCallback?.Invoke(SinkStatus.Closed);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private IWaveePlayerState? PlayingToEndOfTrack()
    {
        if (this.State is WaveePlayingState playingState)
        {
            return new EndOfTrackState
            {
                TrackId = playingState.TrackId,
                PlayRequestId = playingState.PlayRequestId,
                LoadedTrack = new PlayerLoadedTrackData
                {
                    Format = playingState.Format,
                    NormalisationData = playingState.NormalisationData,
                    StreamLoaderController = playingState.StreamLoaderController,
                    AudioItem = playingState.AudioItem,
                    BytesPerSecond = playingState.BytesPerSecond,
                    DurationMs = playingState.DurationMs,
                    StreamPositionMs = playingState.StreamPositionMs,
                    IsExplicit = playingState.IsExplicit
                }
            };
        }

        _logger.LogError("Called PlayingToEndOfTrack in non-playing state: {state}", this.State);
        return null;
    }

    private async Task<PlayerLoadedTrackData>? LoadTrack(string trackId, double positionMs)
    {
        // This method creates a Task that returns the loaded stream and associated info.
        // Ideally all work should be done using asynchronous code. However, seek() on the
        // audio stream is implemented in a blocking fashion. Thus, we can't turn it into future
        // easily. Instead we spawn a thread to do the work and return a one-shot channel as the
        // future to work with.

        // This method creates a task that returns the loaded stream and associated info.
        // Ideally, all work should be done using asynchronous code. However, if the seek() on the
        // audio stream is implemented in a blocking fashion and you can't turn it into an async method,
        // you may use a Task.Run() to run the blocking code on a separate thread.

        var loadHandle = Task.Run(async () =>
        {
            var data = await _trackLoader.LoadTrackAsync(trackId, positionMs);
            if (data != null)
            {
                return data;
            }

            throw new Exception("Loading track failed");
        });

        this.LoadHandles.TryAdd(loadHandle.Id, loadHandle);

        try
        {
            var result = await loadHandle;
            return result;
        }
        catch (Exception? ex)
        {
            // Log the exception if needed
            _logger.LogError(ex, "Error while loading track {trackId}", trackId);
            throw;
        }
        finally
        {
            this.LoadHandles.TryRemove(loadHandle.Id, out _);
        }
    }


    private static string GeneratePlaybackId()
    {
        Span<byte> bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        bytes[0] = 1;
        return bytes.BytesToHex().ToLower();
    }
}

public delegate void SinkEventDelegate(SinkStatus Status);

public enum SinkStatus
{
    Running,
    Closed,
    TemporaryStopped
}

#region PlayerEvents

public interface IWaveePlayerEvent
{
}

public readonly record struct PositionCorrectionEvent(
    string TrackId,
    string PlayRequestId,
    double PositionMs
) : IWaveePlayerEvent;

public readonly record struct EndOfTrackEvent
    (string TrackId, string PlayRequestId) : IWaveePlayerEvent;

public readonly record struct UnavailableEvent(string TrackId, string PlayRequestId) : IWaveePlayerEvent;

public readonly record struct TrackChangedEvent(PlaybackItem Item) : IWaveePlayerEvent;

public readonly record struct PausedPlayerEvent(
    string TrackId,
    string PlayRequestId,
    double PositionMs
) : IWaveePlayerEvent;

public readonly record struct PlayingPlayerEvent(
    string TrackId,
    string PlayRequestId,
    double PositionMs
) : IWaveePlayerEvent;

public readonly record struct LoadingPlayerEvent(
    string TrackId,
    string PlayRequestId,
    double PositionMs
) : IWaveePlayerEvent;

#endregion

#region Player States

public interface IWaveePlayerState
{
}

public readonly record struct InvalidPlayerState() : IWaveePlayerState;

public readonly record struct EndOfTrackState
    (string TrackId, string PlayRequestId, PlayerLoadedTrackData LoadedTrack) : IWaveePlayerState;

public readonly record struct WaveePausedState(
    string TrackId,
    string PlayRequestId,
    IAudioDecoder? Decoder,
    NormalisationData? NormalisationData,
    double NormalisationFactor,
    StreamLoaderController StreamLoaderController,
    int BytesPerSecond,
    double DurationMs,
    double StreamPositionMs,
    bool SuggestedToPreloadNextTrack,
    bool IsExplicit
) : IWaveePlayerState;

public record WaveePlayingState(
    string TrackId,
    string PlayRequestId,
    IAudioDecoder? Decoder,
    IAudioFormat? Format,
    PlaybackItem AudioItem,
    NormalisationData? NormalisationData,
    double NormalisationFactor,
    StreamLoaderController StreamLoaderController,
    int BytesPerSecond,
    double DurationMs,
    double StreamPositionMs,
    DateTimeOffset? ReportedNominalStartTime,
    bool SuggestedToPreloadNextTrack,
    bool IsExplicit
) : IWaveePlayerState
{
    private double _streamPositionMs = StreamPositionMs;
    private DateTimeOffset? _reportedNominalStartTime = ReportedNominalStartTime;

    public double StreamPositionMs
    {
        get => Interlocked.CompareExchange(ref _streamPositionMs, 0, 0);
        set => _streamPositionMs = value;
    }

    public DateTimeOffset? ReportedNominalStartTime
    {
        get => _reportedNominalStartTime;
        set => _reportedNominalStartTime = value;
    }
}

public readonly record struct WaveeLoadingState(
    Task<PlayerLoadedTrackData> Loader,
    string TrackId,
    string PlayRequestId,
    bool StartPlayback
) : IWaveePlayerState;

#endregion

#region Player Commands

public readonly record struct LoadTrackCommand(
    string TrackId,
    string PlayRequestId,
    bool StartPlayback,
    double PositionMs
) : IWaveePlayerCommand;

public interface IWaveePlayerCommand
{
}

#endregion

public static class PlayerStateExtensions
{
    public static bool IsPlaying(this IWaveePlayerState state)
    {
        return state is WaveePlayingState;
    }
}