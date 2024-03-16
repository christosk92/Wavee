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

    //StreamOne is always the main stream, i.g. fading in
    //StreamTwo is always the secondary stream, i.g. fading out
    (WaveeMediaSource, IAudioDecoder)? streamOne = null;
    (WaveeMediaSource, IAudioDecoder)? streamTwo = null;

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
            double? previousDiff = null;
            TimeSpan? previousTime = null;

            const int averageChannels = 2;
            const int averageSampleRate = 44100;
            Span<float> bufferOne = new float[averageChannels * averageSampleRate];
            Span<float> bufferTwo = new float[averageChannels * averageSampleRate];


            while (true)
            {
                try
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
                                Console.WriteLine("new track incoming ");
                                var source = sourceTask.Result;
                                if (source != null)
                                {
                                    Console.WriteLine($"{source.Metadata.Name}");
                                    _latestState = _latestState with
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
                                else
                                {
                                    Console.WriteLine("Source is null");
                                }
                            }
                            else
                            {
                                //TODO: Error
                                var err = sourceTask.AsTask().Exception;
                                Console.WriteLine($"Error occurred: {err.ToString()}");
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
                        if (streamOne is null)
                        {
                            Task.Delay(10).Wait();
                            continue;
                        }

                        var positionMaybe = ReadSamples(streamOne!.Value.Item2, streamTwo?.Item2, bufferOne, bufferTwo);
                        if (!positionMaybe.HasValue)
                        {
                            // End of stream
                            streamOne?.Item1.Dispose();
                            streamOne?.Item2.Dispose();
                            streamOne = null;
                            streamTwo?.Item1.Dispose();
                            streamTwo?.Item2.Dispose();
                            streamTwo = null;
                            Task.Run(NextTrack);
                            continue;
                        }


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
                            Console.WriteLine($"Seek request");

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
                            if (positionMaybe.HasValue)
                            {
                                var position = positionMaybe.Value;
                                if (previousTime is not null)
                                {
                                    var newDiff = (position - previousTime.Value).TotalSeconds;
                                    if (previousDiff is not null)
                                    {
                                        if (Math.Abs(newDiff - previousDiff.Value) > .1)
                                        {
                                            previousDiff = newDiff;
                                            _latestState = _latestState with
                                            {
                                                PositionSinceStartStopwatch = position,
                                                PositionStopwatch = _latestState.Paused
                                                    ? new Stopwatch()
                                                    : Stopwatch.StartNew()
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
                }
                catch (Exception x)
                {
                    Console.WriteLine(x.ToString());
                }
            }
        });
    }

    private void NextTrack()
    {
        using (_stateLock.Lock())
        {
            if (_latestState.Context is not null)
            {
                var currentIndex = _latestState.IndexInContext.Value;
                var nextTrackIndex = currentIndex + 1;

                //TODO: Do not manipulate state directly
                _latestState = _latestState with
                {
                    IndexInContext = nextTrackIndex,
                    PositionSinceStartStopwatch = TimeSpan.Zero,
                    PositionStopwatch = new Stopwatch()
                };
                var next = _latestState.Context.GetAt(nextTrackIndex);
                _nextTrackRequest = (false, _latestState.Context, next, false, TimeSpan.Zero);
            }
        }
    }

    private TimeSpan? ReadSamples(IAudioDecoder streamone,
        IAudioDecoder? streamTwo,
        Span<float> buffer,
        Span<float> bufferTwo)
    {
        var readBytes = streamone.ReadSamples(buffer);
        if (readBytes is 0)
        {
            return null;
        }

        if (streamTwo is not null)
        {
            //TODO: mix samples
        }
        Console.WriteLine($"Reading samples");

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
        Console.WriteLine($"Read samples");
        return streamone.Position;
    }

    private IAudioDecoder CreateAudioStream(WaveeMediaSource source)
    {
        //TODO: Support different formats
        var oggReader = new VorbisWaveReader(source, true);
        return new NAudioDecoder(oggReader, source.Duration);
    }

    public IObservable<WaveePlaybackState> Events => _events.StartWith(_latestState);
    public TimeSpan Position => streamOne?.Item2.Position ?? TimeSpan.Zero;

    public void Play(IWaveePlayContext spotifyPlayContext, int startAt, CancellationToken cancel)
    {
        var sourceTask = spotifyPlayContext.GetAt(startAt, cancel);
        using (_stateLock.Lock())
        {
            //TODO: Do not manipulate state directly
            _latestState = _latestState with
            {
                IndexInContext = startAt,
            };
            _nextTrackRequest = (false, spotifyPlayContext, sourceTask, false, TimeSpan.Zero);
        }
    }
}

public interface IWaveePlayer
{
    IObservable<WaveePlaybackState> Events { get; }
    TimeSpan Position { get; }
    void Play(IWaveePlayContext spotifyPlayContext, int startAt, CancellationToken cancel);
}