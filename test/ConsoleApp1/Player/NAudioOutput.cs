using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NeoSmart.AsyncLock;
using Wavee.Config;
using Wavee.Core.Decoders;
using Wavee.Models.Common;
using Wavee.Playback.Player;
using Wavee.Playback.Streaming;

namespace ConsoleApp1.Player;

internal sealed class NAudioOutput : IAudioOutput, IDisposable
{
    private record MediaHolder(VorbisSampleProvider SampleProvider) : IDisposable
    {
        public void Dispose()
        {
            SampleProvider.Dispose();
        }
    }

    private readonly Dictionary<WaveePlayerMediaItem, MediaHolder> _mediaItems = new();
    private readonly LinkedList<WaveePlayerMediaItem> _cacheOrder = new();
    private readonly Dictionary<WaveePlayerMediaItem, LinkedListNode<WaveePlayerMediaItem>> _cacheNodes = new();
    private readonly AsyncLock _lock = new();

    private WaveePlaybackStream? _previousPlaybackStream;
    private WaveePlaybackStream? _currentPlaybackStream;
    private readonly VolumeSampleProvider _volumeProvider;

    private ISampleProvider? _currentSampleProvider
    {
        get => __currentSampleProvider;
        set
        {
            if (__currentSampleProvider is VorbisSampleProvider v)
            {
                v.EndOfStream -= SampleProviderEndReached;
                v.PrefetchedRequested -= PrefetchRequested;
            }

            if (value is VorbisSampleProvider vn)
            {
                vn.EndOfStream += SampleProviderEndReached;
                vn.PrefetchedRequested += PrefetchRequested;
                vn.TotalTime = _currentPlaybackStream?.SpotifyItem?.Duration ?? TimeSpan.Zero;
            }

            __currentSampleProvider = value;
        }
    }


    public event EventHandler<WaveePlaybackStreamEndedArgs>? MediaEnded;
    public event EventHandler? PrefetchRequested;

    private readonly SpotifyConfig _config;

    private readonly WaveOutEvent _waveOut;
    private readonly MixingSampleProvider _mixer; // To handle switching sample providers
    private ISampleProvider? __currentSampleProvider;

    public NAudioOutput(SpotifyConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));

        // Initialize WaveOutEvent
        _waveOut = new WaveOutEvent();
        _waveOut.PlaybackStopped += OnPlaybackStopped;

        // Initialize mixer with appropriate WaveFormat
        var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        _mixer = new MixingSampleProvider(waveFormat)
        {
            ReadFully = false
        };

        // Wrap the mixer with a VolumeSampleProvider
        _volumeProvider = new VolumeSampleProvider(_mixer)
        {
            Volume = 1.0f // Set default volume to maximum
        };

        // Initialize waveOut with the volume provider
        _waveOut.Init(_volumeProvider);
        _waveOut.Play();
    }

    public int MaxCacheEntries => _config.Playback.MaxActiveNodes;

    /// <summary>
    /// Plays the specified WaveePlaybackStream.
    /// </summary>
    /// <param name="playbackStream">The playback stream to play.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task Play(WaveePlaybackStream playbackStream, CancellationToken cancellationToken)
    {
        if (playbackStream == null)
        {
            throw new ArgumentNullException(nameof(playbackStream));
        }

        MediaHolder? mediaHolder;

        using (await _lock.LockAsync(cancellationToken))
        {
            _manualStop = false;
            // Stop any currently playing media
            if (_currentPlaybackStream != null)
            {
                // _isManualStop = true; // Indicate a manual stop
                _previousPlaybackStream = _currentPlaybackStream;
                _mixer.RemoveAllMixerInputs();
            }

            // Set the current playback stream to the new one
            _currentPlaybackStream = playbackStream;

            // Attempt to retrieve the MediaHolder from the dictionary
            if (_mediaItems.TryGetValue(playbackStream.MediaItem, out mediaHolder))
            {
                // Move the accessed media item to the end to mark it as recently used
                if (_cacheNodes.TryGetValue(playbackStream.MediaItem, out var node))
                {
                    _cacheOrder.Remove(node);
                    _cacheOrder.AddLast(node);
                }

                // Set the current sample provider
                if (mediaHolder.SampleProvider.InnerStream is AudioStream a)
                {
                    _currentPlaybackStream.SpotifyItem = a.Track;
                }

                _currentSampleProvider = mediaHolder.SampleProvider;
                _waveOut.Play();
            }

            // If MediaHolder doesn't exist, create it
            if (mediaHolder == null)
            {
                // Open the stream asynchronously
                var stream = await playbackStream.Open(cancellationToken).ConfigureAwait(false);

                // Create the sample provider
                var sampleProvider = new VorbisSampleProvider(stream);

                mediaHolder = new MediaHolder(sampleProvider);

                lock (_lock)
                {
                    // Add the new MediaHolder to the dictionary
                    _mediaItems[playbackStream.MediaItem] = mediaHolder;

                    // Add the media item to the cache order
                    var node = _cacheOrder.AddLast(playbackStream.MediaItem);
                    _cacheNodes[playbackStream.MediaItem] = node;

                    // Add to mixer
                    _mixer.AddMixerInput(sampleProvider);
                    _currentSampleProvider = sampleProvider;

                    _waveOut.Play();

                    // _isManualStop = false; // Reset the manual stop flag

                    // Check if cache exceeds the maximum allowed entries
                    if (_mediaItems.Count > MaxCacheEntries)
                    {
                        EvictOldestCacheEntry();
                    }
                }
            }
            else
            {
                lock (_lock)
                {
                    // Add the existing sample provider to the mixer
                    _mixer.AddMixerInput(mediaHolder.SampleProvider);
                    _currentSampleProvider = mediaHolder.SampleProvider;
                }
            }
        }
    }

    /// <summary>
    /// Stops any currently playing media.
    /// </summary>
    public void Stop()
    {
        using (_lock.Lock())
        {
            if (_currentPlaybackStream != null &&
                _mediaItems.TryGetValue(_currentPlaybackStream.MediaItem, out var currentHolder))
            {
                _manualStop = true; // Indicate a manual stop
                _mixer.RemoveMixerInput(currentHolder.SampleProvider);
                _currentPlaybackStream = null;
                _currentSampleProvider = null;
            }
        }
    }

    private void SampleProviderEndReached(object? sender, EventArgs e)
    {
        using (_lock.Lock())
        {
            if (_currentPlaybackStream != null)
            {
                MediaEnded?.Invoke(this, new WaveePlaybackStreamEndedArgs(
                    _currentPlaybackStream,
                    WaveePlaybackStreamEndedReasonType.EndOfStream,
                    null
                ));
            }
        }
    }

    private bool _manualStop;

    /// <summary>
    /// Handles the PlaybackStopped event from NAudio's WaveOut.
    /// </summary>
    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        WaveePlaybackStream? endedStream = null;
        using (_lock.Lock())
        {
            if (e.Exception is not null)
            {
                var ars = new WaveePlaybackStreamEndedArgs(
                    _currentPlaybackStream,
                    WaveePlaybackStreamEndedReasonType.Error,
                    e.Exception
                );
                MediaEnded?.Invoke(this, ars);
            }

            if (_manualStop)
            {
                var ars = new WaveePlaybackStreamEndedArgs(
                    _currentPlaybackStream,
                    WaveePlaybackStreamEndedReasonType.ManualStop,
                    e.Exception
                );
                MediaEnded?.Invoke(this, ars);
            }

            //     wasManualStop = _isManualStop;
            //     // Check if the stop was not manual
            //     if (!_isManualStop && _currentPlaybackStream != null)
            //     {
            //         endedStream = _currentPlaybackStream;
            //         _currentPlaybackStream = null;
            //         _currentSampleProvider = null;
            //     }
            //
            //
            //     // Invoke the MediaEnded event outside the lock to prevent potential deadlocks
            //     if (endedStream != null)
            //     {
            //
            // EndedReasonType reason = WaveePlaybackStreamEndedReasonType.EndOfStream;
            //         if (e.Exception != null)
            //         {
            //             reason = WaveePlaybackStreamEndedReasonType.Error;
            //         }
            //         else if (wasManualStop)
            //         {
            //             reason = WaveePlaybackStreamEndedReasonType.ManualStop;
            //         }
            //
            //         var args = new WaveePlaybackStreamEndedArgs(endedStream, reason, e.Exception);
            //         MediaEnded?.Invoke(this, args);
            //     }
            //     else if (wasManualStop)
            //     {
            //         MediaEnded?.Invoke(this, new WaveePlaybackStreamEndedArgs(
            //             _previousPlaybackStream,
            //             WaveePlaybackStreamEndedReasonType.ManualStop,
            //             e.Exception
            //         ));
            //     }
            // }
        }
    }


    /// <summary>
    /// Seeks to the specified position within the current media stream.
    /// </summary>
    /// <param name="position">The target position to seek to.</param>
    public void Seek(TimeSpan position)
    {
        using (_lock.Lock())
        {
            if (_currentSampleProvider is VorbisSampleProvider s)
            {
                try
                {
                    s.Seek(position);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Seek failed: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("No active playback to seek.");
            }
        }
    }

    /// <summary>
    /// Gets the current playback position.
    /// </summary>
    public TimeSpan Position
    {
        get
        {
            using (_lock.Lock())
            {
                if (_currentSampleProvider is VorbisSampleProvider s)
                {
                    return s.Position;
                }
                else
                {
                    return TimeSpan.Zero;
                }
            }
        }
    }

    public WaveePlaybackStream? CurrentPlaybackStream
    {
        get
        {
            using (_lock.Lock())
            {
                return _currentPlaybackStream;
            }
        }
    }

    public bool IsPaused
    {
        get
        {
            using (_lock.Lock())
            {
                return _waveOut.PlaybackState == PlaybackState.Paused;
            }
        }
    }

    public void Resume()
    {
        _waveOut.Play();
    }

    public void Pause()
    {
        _waveOut.Pause();
    }

    public void SetVolume(float volume)
    {
        if (volume < 0f || volume > 1f)
            throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be between 0.0 and 1.0");

        using (_lock.Lock())
        {
            _volumeProvider.Volume = volume;
        }
    }

    public float Volume
    {
        get
        {
            using (_lock.Lock())
            {
                return _volumeProvider.Volume;
            }
        }
    }

    /// <summary>
    /// Evicts the oldest media item from the cache.
    /// </summary>
    private void EvictOldestCacheEntry()
    {
        if (_cacheOrder.First is null)
            return;

        var oldestMediaItem = _cacheOrder.First.Value;

        // Remove from the cache order and cache nodes
        _cacheOrder.RemoveFirst();
        _cacheNodes.Remove(oldestMediaItem);

        // Remove from the media items dictionary and dispose the media holder
        if (_mediaItems.TryGetValue(oldestMediaItem, out var mediaHolder))
        {
            _mediaItems.Remove(oldestMediaItem);
            _mixer.RemoveMixerInput(mediaHolder.SampleProvider);
            mediaHolder.Dispose();
        }
    }

    /// <summary>
    /// Disposes the NAudioOutput and its resources.
    /// </summary>
    public void Dispose()
    {
        // Stop playback and dispose WaveOutEvent
        _waveOut.PlaybackStopped -= OnPlaybackStopped;
        _waveOut.Stop();
        _waveOut.Dispose();

        // Dispose all MediaHolder instances
        using (_lock.Lock())
        {
            foreach (var holder in _mediaItems.Values)
            {
                _mixer.RemoveMixerInput(holder.SampleProvider);
                holder.Dispose();
            }

            _mediaItems.Clear();
            _cacheOrder.Clear();
            _cacheNodes.Clear();
        }
    }
}

internal sealed class WaveePlaybackStreamEndedArgs : EventArgs
{
    public WaveePlaybackStreamEndedArgs(WaveePlaybackStream stream,
        WaveePlaybackStreamEndedReasonType reason,
        Exception? exception)
    {
        Stream = stream;
        Reason = reason;
        Exception = exception;
    }

    public WaveePlaybackStream Stream { get; }
    public WaveePlaybackStreamEndedReasonType Reason { get; }
    public Exception? Exception { get; }
}

internal enum WaveePlaybackStreamEndedReasonType
{
    EndOfStream,
    ManualStop,
    Error
}