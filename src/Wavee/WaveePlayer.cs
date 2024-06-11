using System.Collections.Concurrent;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Wavee.Contracts.Enums;
using Wavee.Contracts.Interfaces;
using Wavee.Contracts.Interfaces.Playback;
using Wavee.Core.Decoders.VorbisDecoder;

namespace Wavee
{
    public sealed class WaveePlayer : IWaveePlayer
    {
        private readonly object _lockObject = new object();
        private bool _isLocked;
        private readonly ConcurrentQueue<Action> _commandQueue = new ConcurrentQueue<Action>();

        private IPlayQueue _queue;
        private IPlayContext _context;
        private RepeatMode _repeatMode;
        private bool _shuffle;
        private bool _isPlaying;
        private IMediaSource _currentTrack;
        private TimeSpan _currentPosition;
        private IWavePlayer _waveOut;
        private ISampleProvider _currentSampleProvider;
        private ISampleProvider _nextSampleProvider;
        private MixingSampleProvider _mixer;

        public IDisposable Lock()
        {
            Monitor.Enter(_lockObject);
            _isLocked = true;

            return new LockRelease(() =>
            {
                _isLocked = false;
                Monitor.Exit(_lockObject);
                ProcessQueue();
            });
        }

        public void Clear()
        {
            EnqueueCommand(() =>
            {
                StopPlayback();
                _queue = null;
                _context = null;
                _repeatMode = RepeatMode.None;
                _shuffle = false;
            });
        }

        public void SetShuffle(bool shuffling)
        {
            EnqueueCommand(() => _shuffle = shuffling);
        }

        public void SetRepeat(RepeatMode repeatContext)
        {
            EnqueueCommand(() => _repeatMode = repeatContext);
        }

        public void Play(Task<IMediaSource> trackTask, TimeSpan position, bool startPlayback)
        {
            EnqueueCommand(async () =>
            {
                try
                {
                    var mediaSource = await trackTask;
                    _currentTrack = mediaSource;
                    _currentPosition = position;

                    if (startPlayback)
                    {
                        await StartPlayback(mediaSource, position);
                    }
                }
                catch (Exception x)
                {
                    // Handle exception
                }
            });
        }

        public void SetQueue(IPlayQueue queue)
        {
            EnqueueCommand(() => _queue = queue);
        }

        public void SetContext(IPlayContext context)
        {
            EnqueueCommand(() => _context = context);
        }

        public void Pause()
        {
            EnqueueCommand(() =>
            {
                _waveOut?.Pause();
                _isPlaying = false;
            });
        }

        public void Resume()
        {
            EnqueueCommand(() =>
            {
                _waveOut?.Play();
                _isPlaying = true;
            });
        }

        public void Stop()
        {
            EnqueueCommand(() =>
            {
                StopPlayback();
                _isPlaying = false;
            });
        }

        public void Skip()
        {
            EnqueueCommand(async () => { await PlayNextTrack(); });
        }

        public void Previous()
        {
            EnqueueCommand(async () => { await PlayPreviousTrack(); });
        }

        public void Seek(TimeSpan position)
        {
            EnqueueCommand(() =>
            {
                if (_waveOut != null && _currentSampleProvider != null)
                {
                    _currentPosition = position;
                    // Handle seeking logic in the decoded stream (if supported)
                }
            });
        }

        private void EnqueueCommand(Action command)
        {
            _commandQueue.Enqueue(command);
            ProcessQueue();
        }

        private void ProcessQueue()
        {
            if (!_isLocked)
            {
                while (_commandQueue.TryDequeue(out var command))
                {
                    command();
                }
            }
        }

        private async Task StartPlayback(IMediaSource mediaSource, TimeSpan position)
        {
            StopPlayback();

            var stream = await mediaSource.CreateStream(CancellationToken.None);
            _currentSampleProvider = await DecodeStream(stream);

            if (_waveOut == null)
            {
                _waveOut = new WaveOutEvent();
                _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2))
                {
                    ReadFully = true
                };
                _waveOut.Init(_mixer);
            }

            _mixer.AddMixerInput(_currentSampleProvider);
            _waveOut.Play();
            _isPlaying = true;

            HandleTrackEnd();
        }

        private async Task<ISampleProvider> DecodeStream(Stream stream)
        {
            var reader = new VorbisWaveReader(stream, false);
            //  var reader = new Mp3FileReader(stream); // Assume MP3 for this example
            var sampleProvider = reader.ToSampleProvider();
            return sampleProvider;
        }

        private void HandleTrackEnd()
        {
            Task.Run(async () =>
            {
                while (_isPlaying)
                {
                    if (_waveOut.PlaybackState == PlaybackState.Stopped)
                    {
                        await PlayNextTrack();
                    }

                    await Task.Delay(1000); // Check every second
                }
            });
        }

        private async Task PlayNextTrack()
        {
            IMediaSource nextTrack = null;

            if (_queue != null && _queue.HasNext())
            {
                nextTrack = _queue.NextTrack(_shuffle);
            }
            else if (_context != null && _context.HasNext())
            {
                nextTrack = _context.NextTrack(_shuffle);
            }

            if (nextTrack != null)
            {
                await StartPlayback(nextTrack, TimeSpan.Zero);
            }
            else if (_repeatMode == RepeatMode.RepeatContext)
            {
                ResetToFirstTrack();
            }
            else
            {
                StopPlayback();
            }
        }

        private async Task PlayPreviousTrack()
        {
            IMediaSource previousTrack = null;

            if (_queue != null && _queue.HasPrevious())
            {
                previousTrack = _queue.PreviousTrack();
            }
            else if (_context != null && _context.HasPrevious())
            {
                previousTrack = _context.PreviousTrack();
            }

            if (previousTrack != null)
            {
                await StartPlayback(previousTrack, TimeSpan.Zero);
            }
            else if (_repeatMode == RepeatMode.RepeatContext)
            {
                ResetToLastTrack();
            }
            else
            {
                StopPlayback();
            }
        }

        private void ResetToFirstTrack()
        {
            EnqueueCommand(async () =>
            {
                if (_context != null)
                {
                    _context.ResetToFirst();
                    await PlayNextTrack();
                }
            });
        }

        private void ResetToLastTrack()
        {
            EnqueueCommand(async () =>
            {
                if (_context != null)
                {
                    _context.ResetToLast();
                    await PlayPreviousTrack();
                }
            });
        }

        private void StopPlayback()
        {
            if (_waveOut != null)
            {
                _waveOut.Stop();
                _mixer.RemoveAllMixerInputs();
            }

            _isPlaying = false;
        }

        private class LockRelease : IDisposable
        {
            private readonly Action _releaseAction;
            private bool _disposed;

            public LockRelease(Action releaseAction)
            {
                _releaseAction = releaseAction;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _releaseAction();
                    _disposed = true;
                }
            }
        }
    }
}