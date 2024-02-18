using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using AsyncKeyedLock;
using NAudio.Wave;
using Wavee.Core.Decoders.VorbisDecoder;

namespace Wavee.Core;

public sealed class WaveePlayer : IWaveePlayer
{
    private readonly Subject<WaveePlaybackState> _events = new();
    private readonly AsyncNonKeyedLocker _stateLock = new();

    private (bool fadingIn, IWaveePlayContext Context, ValueTask<WaveeMediaSource?>, bool StartPause, TimeSpan StartAt)?
        _nextTrackRequest;

    private TimeSpan? seekRequest = null;
    private bool? pauseRequest = null;

    private WaveePlaybackState _latestState;
    private readonly WaveOutEvent _wavePlayer;

    private BufferedWaveProvider? _bufferedWaveProvider;
    private WaveFormat? _waveFormat;

    public WaveePlayer()
    {
        _wavePlayer = new WaveOutEvent();
        _latestState = new WaveePlaybackState
        {
            PositionSinceStartStopwatch = TimeSpan.Zero,
            PositionStopwatch = new Stopwatch()
        };

        Task.Run(() =>
        {
            // In the event of crossfade:
            //StreamOne is always the main stream, i.g. fading in
            //StreamTwo is always the secondary stream, i.g. fading out
            (WaveeMediaSource, IAudioDecoder)? streamOne = null;
            (WaveeMediaSource, IAudioDecoder)? streamTwo = null;
            double? previousDiff = null;
            TimeSpan? previousTime = null;

            const int averageChannels = 2;
            const int averageSampleRate = 44100;
            Span<float> bufferOne = new float[averageChannels * averageSampleRate];
            Span<float> bufferTwo = new float[averageChannels * averageSampleRate];


            while (true)
            {
                using (_stateLock.Lock())
                {
                    if (_nextTrackRequest.HasValue)
                    {
                        var (fadingIn, ctx, sourceTask, startPaused, startAt) = _nextTrackRequest.Value;
                        if (!sourceTask.IsCompleted)
                        {
                            // We're still waiting for the source to be ready
                            if (_latestState.Source is not null)
                            {
                                if (streamOne is not null && streamOne.Value.Item1 == _latestState.Source)
                                {
                                    if (fadingIn)
                                    {
                                        //dont immediately close stream
                                    }
                                    else
                                    {
                                        streamOne.Value.Item1.Dispose();
                                        streamOne.Value.Item2.Dispose();
                                        streamOne = null;
                                    }
                                }
                            }

                            var newState = _latestState with
                            {
                                Context = ctx,
                                IsBuffering = true,
                                EndOfContextReached = false,
                                IsActive = true,
                                Paused = startPaused,
                                PositionStopwatch = new Stopwatch(),
                                PositionSinceStartStopwatch = TimeSpan.Zero,
                                RepeatState = _latestState.RepeatState,
                                ShuffleState = _latestState.ShuffleState,
                                Source = null
                            };
                            if (newState.IsBuffering != _latestState.IsBuffering)
                            {
                                _latestState = newState;
                                _events.OnNext(newState);
                            }

                            Task.Delay(10).Wait();
                            continue;
                        }

                        if (sourceTask.IsCompletedSuccessfully)
                        {
                            var source = sourceTask.Result;
                            if (source != null)
                            {
                                _latestState = new WaveePlaybackState
                                {
                                    IsActive = true,
                                    Source = source,
                                    EndOfContextReached = false,
                                    Context = ctx,
                                    Paused = startPaused,
                                    IsBuffering = false,
                                    RepeatState = _latestState.RepeatState,
                                    ShuffleState = _latestState.ShuffleState,
                                    PositionStopwatch = startPaused ? new Stopwatch() : Stopwatch.StartNew(),
                                    PositionSinceStartStopwatch = startAt
                                };
                                previousTime = null;
                                previousDiff = null;
                                if (fadingIn)
                                {
                                    // Swap the streams
                                    if (streamTwo is not null)
                                    {
                                        streamTwo.Value.Item1.Dispose();
                                        streamTwo.Value.Item2.Dispose();
                                    }

                                    streamTwo = streamOne;
                                    streamOne = (source, CreateAudioStream(source));
                                }
                                else
                                {
                                    streamOne?.Item1.Dispose();
                                    streamOne?.Item2.Dispose();
                                    streamTwo?.Item1.Dispose();
                                    streamTwo?.Item2.Dispose();

                                    streamOne = (source, CreateAudioStream(source));
                                    streamTwo = null;
                                }

                                _events.OnNext(_latestState);
                            }
                        }
                        else
                        {
                            //TODO: Error
                            var err = sourceTask.AsTask().Exception;
                            Debugger.Break();
                            continue;
                        }

                        _nextTrackRequest = null;
                    }

                    if (_latestState.Source is null)
                    {
                        Task.Delay(10).Wait();
                        continue;
                    }


                    if (_latestState.Paused)
                    {
                        Task.Delay(10).Wait();
                        continue;
                    }

                    var position = ReadSamples(streamOne!.Value.Item2, streamTwo?.Item2, bufferOne, bufferTwo);

                    if (pauseRequest is not null)
                    {
                        if (pauseRequest.Value)
                        {
                            _latestState = _latestState with { Paused = true };
                            _events.OnNext(_latestState);
                        }
                        else
                        {
                            _latestState = _latestState with { Paused = false };
                            _events.OnNext(_latestState);
                        }
                    }

                    if (seekRequest.HasValue)
                    {
                        streamOne.Value.Item2.Seek(seekRequest.Value);
                        _events.OnNext(_latestState with
                        {
                            PositionSinceStartStopwatch = seekRequest.Value,
                            PositionStopwatch = _latestState.Paused ? new Stopwatch() : Stopwatch.StartNew()
                        });
                        seekRequest = null;
                    }
                    else
                    {
                        //check if position changed
                        if (previousTime is not null)
                        {
                            var newDiff = (position - previousTime.Value).TotalSeconds;
                            if (previousDiff is not null)
                            {
                                if (Math.Abs(newDiff - previousDiff.Value) > 0.1)
                                {
                                    previousDiff = newDiff;
                                    _latestState = _latestState with
                                    {
                                        PositionSinceStartStopwatch = position,
                                        PositionStopwatch = _latestState.Paused ? new Stopwatch() : Stopwatch.StartNew()
                                    };
                                    _events.OnNext(_latestState);
                                }
                            }
                            else
                            {
                                previousDiff = newDiff;
                            }
                        }
                        
                        previousTime = position;
                    }
                }
            }
        });
    }

    private TimeSpan ReadSamples(IAudioDecoder streamone,
        IAudioDecoder? streamTwo,
        Span<float> buffer,
        Span<float> bufferTwo)
    {
        var streamOnePosition = streamone.ReadSamples(buffer);
        if (streamTwo is not null)
        {
            //TODO: mix samples
        }

        // write
        if (_bufferedWaveProvider is null)
        {
            _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(streamone.SampleRate, streamone.Channels);
            _bufferedWaveProvider = new BufferedWaveProvider(_waveFormat);
            _wavePlayer.Init(_bufferedWaveProvider);
            _wavePlayer.Play();
        }

        var bytes = MemoryMarshal.Cast<float, byte>(buffer).ToArray();
        _bufferedWaveProvider.AddSamples(bytes, 0, bytes.Length);
        while (_bufferedWaveProvider.BufferedDuration.TotalSeconds > 0.5)
        {
            Thread.Sleep(5);
        }

        return streamone.Position;
    }

    private IAudioDecoder CreateAudioStream(WaveeMediaSource source)
    {
        //TODO: Support different formats
        var oggReader = new VorbisWaveReader(source, true);
        return new NAudioDecoder(oggReader, source.Duration);
    }

    public IObservable<WaveePlaybackState> Events => _events.StartWith(_latestState);

    public void Play(IWaveePlayContext spotifyPlayContext, int startAt, CancellationToken cancel)
    {
        var sourceTask = spotifyPlayContext.GetAt(startAt, cancel);
        using (_stateLock.Lock())
        {
            _nextTrackRequest = (false, spotifyPlayContext, sourceTask, false, TimeSpan.Zero);
        }
    }
}

public interface IWaveePlayer
{
    IObservable<WaveePlaybackState> Events { get; }
    void Play(IWaveePlayContext spotifyPlayContext, int startAt, CancellationToken cancel);
}