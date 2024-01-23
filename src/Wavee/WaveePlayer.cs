using System.Diagnostics;
using System.Runtime.InteropServices;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using NAudio.Wave;
using Serilog;
using Wavee.Contexting;

namespace Wavee;

public sealed class WaveeContextStream
{
    public WaveeContextStream(WaveeStream stream, ComposedKey idInContext)
    {
        Stream = stream;
        IdInContext = idInContext;
    }

    public ComposedKey IdInContext { get; }
    public WaveeStream Stream { get; }
}

public sealed class WaveeStream : IDisposable, IAsyncDisposable
{
    public WaveeStream(WaveStream audioStream, IWaveePlayableItem metadata)
    {
        AudioStream = audioStream;
        Metadata = metadata;
    }

    public WaveStream AudioStream { get; }
    public IWaveePlayableItem Metadata { get; }

    public void Dispose()
    {
        AudioStream.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await AudioStream.DisposeAsync();
    }
}

public sealed class WaveePlayer
{
    private IWaveePlayerContext? _currentContext;

    private bool _closed;
    private readonly WriteSamples _writeSamples;

    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private WaveePlayerState _state;

    public WaveePlayer(ILogger logger)
    {
        // TODO: Write Samples
        var waveOut = new WaveOutEvent();
        var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        var floatWaveProvider = new BufferedWaveProvider(waveFormat);
        floatWaveProvider.BufferDuration = TimeSpan.FromSeconds(30); // allow us to get well ahead of ourselves   

        waveOut.Init(floatWaveProvider);
        waveOut.Play();

        _writeSamples = (samples, sampleRate, channels, waitForConsumptionOption) =>
        {
            if (waitForConsumptionOption.Wait)
            {
                floatWaveProvider.AddSamples(samples.ToArray(), 0, samples.Length);

                var samplesToWaitFor = (int)(samples.Length * waitForConsumptionOption.FractionOfSamplesToWaitFor);
                while (floatWaveProvider.BufferedBytes > samplesToWaitFor)
                {
                    Task.Delay(1).Wait();
                }
            }
            else
            {
                floatWaveProvider.AddSamples(samples.ToArray(), 0, samples.Length);
            }
        };

        var runningThread = new Thread(() =>
        {
            //Create an outer loop to catch exceptions
            // So we save performance by not having to check for exceptions on every iteration (every 10ms)
            // Instead, the inner loop can freely do its own thing, and if it throws, we catch it here.
            Span<byte> streamOneBuffer = stackalloc byte[1024 * 64];
            Span<byte> streamTwoBuffer = stackalloc byte[1024 * 64];
            Span<byte> mixBuffer = stackalloc byte[1024 * 64];
            IWaveePlayableItem? lastTrack = null;
            while (!_closed)
            {
                try
                {
                    // Inner loop:
                    while (!_closed)
                    {
                        bool trackChanged = false;
                        bool pausedChanged = false;
                        bool seekChanged = false;

                        _semaphore.Wait();

                        if (_state.SeekRequest.IsSome)
                        {
                            _state.StreamOne.ValueUnsafe().Stream.AudioStream.CurrentTime =
                                _state.SeekRequest.ValueUnsafe();
                            _state = _state with
                            {
                                SeekRequest = Option<TimeSpan>.None
                            };

                            seekChanged = true;
                        }


                        if (!Paused)
                        {
                            var newState = Loop(_state, _writeSamples, streamOneBuffer, streamTwoBuffer, mixBuffer);
                            _state = newState;
                        }

                        if (_state.RequestNextStream)
                        {
                            _state = _state with
                            {
                                RequestNextStream = false,
                                NextStream = Option<ValueTask<Option<WaveeContextStream>>>.None
                            };
                            Task.Run(async () => await RequestNextStream());
                        }

                        if (_state.StreamOne.IsSome && lastTrack != _state.StreamOne.ValueUnsafe().Stream.Metadata)
                        {
                            lastTrack = _state.StreamOne.ValueUnsafe().Stream.Metadata;
                            trackChanged = true;
                        }

                        if (_state.PauseRequest.IsSome)
                        {
                            if (_state.PauseRequest.ValueUnsafe())
                            {
                                Paused = true;
                                waveOut.Pause();
                            }
                            else
                            {
                                Paused = false;
                                waveOut.Play();
                            }

                            _state = _state with
                            {
                                PauseRequest = Option<bool>.None
                            };
                            pausedChanged = true;
                            // PausedChanged?.Invoke(this, _state.PauseRequest.ValueUnsafe());
                        }

                        _semaphore.Release();

                        if (trackChanged)
                        {
                            TrackChanged?.Invoke(this, _state.StreamOne.ValueUnsafe().Stream.Metadata);
                        }

                        if (pausedChanged)
                        {
                            PausedChanged?.Invoke(this, Paused);
                        }

                        if (seekChanged && _state.StreamOne.IsSome)
                        {
                            PositionChanged?.Invoke(this,
                                new WaveePlayerPositionChangedEventArgs(
                                    _state.StreamOne.ValueUnsafe().Stream.AudioStream.CurrentTime,
                                    WaveePlayerPositionChangedEventType.UserRequestedSeeked));
                        }

                        if (_state.StreamOne.IsSome)
                        {
                            PositionChanged?.Invoke(this,
                                new WaveePlayerPositionChangedEventArgs(
                                    _state.StreamOne.ValueUnsafe().Stream.AudioStream.CurrentTime,
                                    WaveePlayerPositionChangedEventType.Playback));
                        }

                        Task.Delay(1).Wait();
                    }
                }
                catch (Exception e)
                {
                    logger.Error(e, "Error in WaveePlayer loop");
                }
                finally
                {
                    try
                    {
                        _semaphore.Release();
                    }
                    catch (Exception)
                    {
                        // Swallow
                    }
                }
            }
        });

        runningThread.Start();
    }

    public Option<TimeSpan> Position
    {
        get
        {
            _semaphore.Wait();
            var position = _state.StreamOne.Match(
                Some: x => x.Stream.AudioStream.CurrentTime,
                None: () => TimeSpan.Zero);
            _semaphore.Release();
            return position;
        }
    }

    public Option<TimeSpan> Duration
    {
        get
        {
            _semaphore.Wait();
            var duration = _state.StreamOne.Match(
                Some: x => x.Stream.AudioStream.TotalTime,
                None: () => TimeSpan.Zero);
            _semaphore.Release();
            return duration;
        }
    }

    public Option<IWaveePlayerContext> Context => _currentContext is null
        ? Option<IWaveePlayerContext>.None
        : Option<IWaveePlayerContext>.Some(_currentContext);

    public Option<WaveeContextStream> CurrentStream
    {
        get
        {
            _semaphore.Wait();
            var stream = _state.StreamOne;
            _semaphore.Release();
            return stream;
        }
    }

    public bool Paused { get; private set; }

    public event EventHandler<IWaveePlayableItem>? TrackChanged;
    public event EventHandler<WaveePlayerPositionChangedEventArgs> PositionChanged;
    public event EventHandler<bool> PausedChanged;

    public async ValueTask Play(WaveeContextStream stream, IWaveePlayerContext context)
    {
        await _semaphore.WaitAsync();
        _currentContext = context;
        try
        {
            if (_state.StreamOne.IsSome)
            {
                await _state.StreamOne.ValueUnsafe().Stream.DisposeAsync();
            }

            if (_state.StreamTwo.IsSome)
            {
                await _state.StreamTwo.ValueUnsafe().Stream.DisposeAsync();
            }

            _state = new WaveePlayerState(
                StreamOne: Option<WaveeContextStream>.Some(stream),
                StreamTwo: Option<WaveeContextStream>.None,
                CrossFadeDuration: Option<TimeSpan>.None,
                NextStream: Option<ValueTask<Option<WaveeContextStream>>>.None,
                RequestNextStream: false,
                SeekRequest: Option<TimeSpan>.None,
                PauseRequest: Option<bool>.None
            );
        }
        catch (Exception)
        {
            // Swallow
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask Play(IWaveePlayerContext ctx)
    {
        var stream = await ctx.GetNextStream();
        if (stream.IsNone)
        {
            // TODO: Should we clear the stack?
            return;
        }

        await _semaphore.WaitAsync();
        _currentContext = ctx;
        try
        {
            if (_state.StreamOne.IsSome)
            {
                await _state.StreamOne.ValueUnsafe().Stream.DisposeAsync();
            }

            if (_state.StreamTwo.IsSome)
            {
                await _state.StreamTwo.ValueUnsafe().Stream.DisposeAsync();
            }

            _state = new WaveePlayerState(
                StreamOne: Option<WaveeContextStream>.Some(stream.ValueUnsafe()),
                StreamTwo: Option<WaveeContextStream>.None,
                CrossFadeDuration: Option<TimeSpan>.None,
                NextStream: Option<ValueTask<Option<WaveeContextStream>>>.None,
                RequestNextStream: false,
                SeekRequest: Option<TimeSpan>.None,
                PauseRequest: Option<bool>.None
            );
        }
        catch (Exception)
        {
            // Swallow
        }
        finally
        {
            _semaphore.Release();
        }
    }

    // public async Task Play(Option<TimeSpan> crossFadeDuration, params Func<ValueTask<WaveeStream>>[] streams)
    // {
    //     _semaphore.Wait();
    //     var firstStream = await streams[0]();
    //
    //     _state = new WaveePlayerState(
    //         StreamOne: Option<WaveeStream>.Some(firstStream),
    //         StreamTwo: Option<WaveeStream>.None,
    //         CrossFadeDuration: crossFadeDuration,
    //         NextStream: Option<ValueTask<WaveeStream>>.None,
    //         RequestNextStream: false,
    //         SeekRequest: Option<TimeSpan>.None,
    //         PauseRequest: Option<bool>.None
    //     );
    //
    //     for (var i = 1; i < streams.Length; i++)
    //     {
    //         _streamStack.Push(streams[i]);
    //     }
    //
    //     _semaphore.Release();
    // }

    private ValueTask RequestNextStream()
    {
        if (_currentContext is null)
        {
            //TODO: maybe pop queue! 
            return ValueTask.CompletedTask;
        }

        _semaphore.Wait();
        var nextStreamTask = _currentContext.GetNextStream();

        _state = _state with
        {
            NextStream = Option<ValueTask<Option<WaveeContextStream>>>.Some(nextStreamTask)
        };

        _semaphore.Release();

        return ValueTask.CompletedTask;
    }

    private static WaveePlayerState Loop(WaveePlayerState state, WriteSamples writeSamples,
        Span<byte> streamOneBuffer,
        Span<byte> streamTwoBuffer,
        Span<byte> mixBuffer)
    {
        // Read samples
        // Mix samples
        // Write to buffer
        var crossFadeDuration = state.CrossFadeDuration;

        // Check if the next stream has been fetched
        if (state.NextStream.IsSome)
        {
            // We have a next stream, check if it has been fetched
            var nextStream = state.NextStream.ValueUnsafe();
            if (nextStream.IsCompletedSuccessfully)
            {
                if (nextStream.Result.IsSome)
                {
                    var nextStreamVal = nextStream.Result.ValueUnsafe();
                    // We have a next stream, and it has been fetched
                    // Swap streams
                    // Next stream becomes main stream
                    // Current main stream becomes crossfade stream
                    // Next stream becomes none
                    state = state with
                    {
                        StreamOne = nextStreamVal,
                        StreamTwo = state.StreamOne,
                        NextStream = Option<ValueTask<Option<WaveeContextStream>>>.None
                    };
                }

                {
                }
            }
            else if (nextStream.IsFaulted)
            {
                Exception ex = null!;
                try
                {
                    _ = nextStream.Result;
                }
                catch (Exception x)
                {
                    // Swallow
                    ex = x;
                }

                Debugger.Break();
            }
        }

        if (state.StreamOne.IsNone && state.StreamTwo.IsNone)
        {
            return state;
        }

        if (state.StreamOne.IsNone)
        {
            // We need a first stream.. Invalid state
            return state;
        }

        var firstStreamVal = state.StreamOne.ValueUnsafe();
        // Read samples from firstStream
        var firstStreamReadResult = firstStreamVal.Stream.AudioStream.Read(streamOneBuffer);
        bool reachedEnd = false;
        if (firstStreamReadResult is 0 || ReachedEndOf(firstStreamVal.Stream))
        {
            reachedEnd = true;
            firstStreamVal.Stream.Dispose();
            state = state with
            {
                StreamOne = Option<WaveeContextStream>.None
            };
            GC.Collect();
        }

        Option<int> secondStreamReadResult = Option<int>.None;
        if (state.StreamTwo.IsSome)
        {
            // Read samples from secondStream
            secondStreamReadResult = state.StreamTwo.ValueUnsafe().Stream.AudioStream.Read(streamTwoBuffer);
            if (secondStreamReadResult.ValueUnsafe() is 0 || ReachedEndOf(state.StreamTwo.ValueUnsafe().Stream))
            {
                // Stream has ended
                // Dispose
                state.StreamTwo.ValueUnsafe().Stream.Dispose();
                state = state with
                {
                    StreamTwo = Option<WaveeContextStream>.None
                };
                GC.Collect();
            }
        }

        // Mix samples
        var fadeInFactor =
            CalculateFadeInFactor(crossFadeDuration, firstStreamVal.Stream.AudioStream.CurrentTime,
                firstStreamVal.Stream.AudioStream.TotalTime);
        var fadeOutFactor = state.StreamTwo.IsSome
            ? CalculateFadeOutFactor(crossFadeDuration, state.StreamTwo.ValueUnsafe().Stream.AudioStream.CurrentTime,
                state.StreamTwo.ValueUnsafe().Stream.AudioStream.TotalTime)
            : 0f;
        if (secondStreamReadResult.IsSome)
        {
            // Convert streamOneBuffer to float
            // Convert streamTwoBuffer to float
            // Mix
            // Convert mixBuffer to byte

            var streamOneFloatBuffer =
                MemoryMarshal.Cast<byte, float>(streamOneBuffer[..firstStreamReadResult]);
            var streamTwoFloatBuffer =
                MemoryMarshal.Cast<byte, float>(streamTwoBuffer[..secondStreamReadResult.ValueUnsafe()]);
            var mixFloatBuffer =
                MemoryMarshal.Cast<byte, float>(
                    mixBuffer[..Math.Max(firstStreamReadResult, secondStreamReadResult.ValueUnsafe())]);
            for (var i = 0; i < mixFloatBuffer.Length; i++)
            {
                mixFloatBuffer[i] =
                    streamOneFloatBuffer[i] * fadeInFactor + streamTwoFloatBuffer[i] * fadeOutFactor;
            }

            var mixByteBuffer = MemoryMarshal.Cast<float, byte>(mixFloatBuffer);
            writeSamples(mixByteBuffer, firstStreamVal.Stream.AudioStream.WaveFormat.SampleRate,
                firstStreamVal.Stream.AudioStream.WaveFormat.Channels, All);
        }
        else
        {
            // Write streamOne
            writeSamples(streamOneBuffer[..firstStreamReadResult],
                firstStreamVal.Stream.AudioStream.WaveFormat.SampleRate,
                firstStreamVal.Stream.AudioStream.WaveFormat.Channels, All);
        }

        // Check if we reached the end of the first stream or if we need crossfade
        if (reachedEnd)
        {
            state = state with
            {
                RequestNextStream = true
            };
        }
        else if (state.NextStream.IsNone && ReachedCrossFadePoint(firstStreamVal.Stream.AudioStream, crossFadeDuration))
        {
            // Crossfade
            state = state with
            {
                RequestNextStream = true
            };
        }
        else if (state.NextStream.IsNone && (firstStreamReadResult is 0 || ReachedEndOf(firstStreamVal.Stream)))
        {
            // We reached the end of the first stream, request next stream
            state = state with
            {
                RequestNextStream = true
            };
        }

        if (state.StreamTwo.IsSome &&
            (secondStreamReadResult.ValueUnsafe() is 0 || ReachedEndOf(state.StreamTwo.ValueUnsafe().Stream)))
        {
            state = state with
            {
                StreamTwo = Option<WaveeContextStream>.None
            };
        }


        return state;
    }

    private static bool ReachedEndOf(WaveeStream x)
    {
        return x.AudioStream.CurrentTime >= x.AudioStream.TotalTime;
    }

    private static bool ReachedCrossFadePoint(WaveStream firstStreamVal, Option<TimeSpan> crossFadeDuration)
    {
        if (crossFadeDuration.IsNone)
        {
            return false;
        }

        var currentTime = firstStreamVal.CurrentTime;
        var totalTime = firstStreamVal.TotalTime;
        var crossFadeDurationVal = crossFadeDuration.ValueUnsafe();
        return currentTime + crossFadeDurationVal >= totalTime;
    }

    private static float CalculateFadeOutFactor(Option<TimeSpan> crossFadeDuration, TimeSpan currentTime,
        TimeSpan totalTime)
    {
        if (crossFadeDuration.IsNone)
        {
            return 1f;
        }

        var crossFadeDurationVal = crossFadeDuration.ValueUnsafe();
        var max = Math.Min(totalTime.TotalMilliseconds, crossFadeDurationVal.TotalMilliseconds);

        //Lets say we have a crossfade duration of 10 seconds, and the total time is 60 seconds, and the current time is 55 seconds, then we want to return 0.5
        return Math.Clamp((float)((totalTime.TotalMilliseconds - currentTime.TotalMilliseconds) / max), 0f, 1f);
    }

    private static float CalculateFadeInFactor(Option<TimeSpan> crossFadeDuration, TimeSpan currentTime,
        TimeSpan totalTime)
    {
        // Linear fade in
        if (crossFadeDuration.IsNone)
        {
            return 1f;
        }

        var crossFadeDurationVal = crossFadeDuration.ValueUnsafe();
        var max = Math.Min(totalTime.TotalMilliseconds, crossFadeDurationVal.TotalMilliseconds);

        //Lets say we have a crossfade duration of 10 seconds, and the current time is 5 seconds, then we want to return 0.5 
        return Math.Clamp((float)(currentTime.TotalMilliseconds / max), 0f, 1f);
    }

    private static readonly WaitForConsumptionOption All = new(true, 1);
    private static readonly WaitForConsumptionOption ThreeQuarters = new(true, 0.75f);
    private static readonly WaitForConsumptionOption Half = new(true, 0.5f);
    private static readonly WaitForConsumptionOption Quarter = new(true, 0.25f);
    private static readonly WaitForConsumptionOption Eighth = new(true, 0.125f);
    private static readonly WaitForConsumptionOption Tenth = new(true, 0.1f);
    private static readonly WaitForConsumptionOption None = new(false, 0);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="StreamOne">
    /// StreamOne is always the main stream, this is the track that is also displayed in the UI.
    /// </param>
    /// <param name="StreamTwo">
    /// StreamTwo is the stream that is fading in or out. This is the track that is not displayed in the UI.
    ///
    /// Ex 1: When fading in, StreamOne is the track is fading in, and StreamTwo is the track that is fading out.
    /// Ex 2: When fading out, StreamTwo is the track fading out, and StreamOne is the track fading in.
    ///
    /// If crossfade starts, swap the streams.
    /// </param>
    /// <param name="CrossFadeDuration"></param>
    /// <param name="RequestNextStream">
    ///  This will be set to true when we want StreamTwo to be filled with the next track.
    /// </param>
    private readonly record struct WaveePlayerState(
        Option<WaveeContextStream> StreamOne,
        Option<WaveeContextStream> StreamTwo,
        Option<TimeSpan> CrossFadeDuration,
        Option<ValueTask<Option<WaveeContextStream>>> NextStream,
        bool RequestNextStream,
        Option<TimeSpan> SeekRequest,
        Option<bool> PauseRequest);

    public void SeekAsFraction(double fraction)
    {
        _semaphore.Wait();
        if (_state.StreamOne.IsSome)
        {
            _state = _state with
            {
                SeekRequest = Option<TimeSpan>.Some(TimeSpan.FromMilliseconds(
                    fraction * _state.StreamOne.ValueUnsafe().Stream.AudioStream.TotalTime.TotalMilliseconds))
            };
        }

        _semaphore.Release();
    }

    public void Resume()
    {
        _semaphore.Wait();
        _state = _state with
        {
            PauseRequest = Option<bool>.Some(false)
        };
        _semaphore.Release();
    }

    public void Pause()
    {
        _semaphore.Wait();
        _state = _state with
        {
            PauseRequest = Option<bool>.Some(true)
        };
        _semaphore.Release();
    }

    public void SeekToPosition(TimeSpan fromMilliseconds)
    {
        _semaphore.Wait();
        if (_state.StreamOne.IsNone)
        {
            _semaphore.Release();
            return;
        }

        var max = Math.Min(_state.StreamOne.ValueUnsafe().Stream.AudioStream.TotalTime.TotalMilliseconds,
            fromMilliseconds.TotalMilliseconds);
        //subtract 10ms
        var min = Math.Max(0, fromMilliseconds.TotalMilliseconds - 10);
        fromMilliseconds = TimeSpan.FromMilliseconds(min);
        _state = _state with
        {
            SeekRequest = Option<TimeSpan>.Some(fromMilliseconds)
        };
        _semaphore.Release();
    }

    public void Stop()
    {
        _semaphore.Wait();
        if (_state.StreamOne.IsSome)
        {
            _state.StreamOne.ValueUnsafe().Stream.Dispose();
        }

        if (_state.StreamTwo.IsSome)
        {
            _state.StreamTwo.ValueUnsafe().Stream.Dispose();
        }

        _state = new WaveePlayerState(
            StreamOne: Option<WaveeContextStream>.None,
            StreamTwo: Option<WaveeContextStream>.None,
            CrossFadeDuration: Option<TimeSpan>.None,
            NextStream: Option<ValueTask<Option<WaveeContextStream>>>.None,
            RequestNextStream: false,
            SeekRequest: Option<TimeSpan>.None,
            PauseRequest: Option<bool>.None
        );

        _semaphore.Release();
    }

    public async ValueTask<bool> PlayWithinContext(int index)
    {
        if (_currentContext is null)
        {
            return false;
        }

        var minusOneMax = Math.Max(0, index - 1);
        var moved = await _currentContext.TrySkip(minusOneMax);
        if (moved)
        {
            await _semaphore.WaitAsync();
            if (_state.StreamOne.IsSome)
            {
                await _state.StreamOne.ValueUnsafe().Stream.DisposeAsync();
            }

            if (_state.StreamTwo.IsSome)
            {
                await _state.StreamTwo.ValueUnsafe().Stream.DisposeAsync();
            }

            _state = _state with
            {
                NextStream = Option<ValueTask<Option<WaveeContextStream>>>.Some(_currentContext.GetNextStream()),
                StreamOne = Option<WaveeContextStream>.None,
                StreamTwo = Option<WaveeContextStream>.None,
                RequestNextStream = false,
                SeekRequest = Option<TimeSpan>.None,
                PauseRequest = Option<bool>.None
            };
            _semaphore.Release();

            return true;
        }

        return false;
    }
}

public sealed class WaveePlayerPositionChangedEventArgs : EventArgs
{
    public WaveePlayerPositionChangedEventArgs(TimeSpan position, WaveePlayerPositionChangedEventType reason)
    {
        Position = position;
        Reason = reason;
    }

    public TimeSpan Position { get; }
    public WaveePlayerPositionChangedEventType Reason { get; }
}

public enum WaveePlayerPositionChangedEventType
{
    /// <summary>
    /// Position changed because of playback.
    ///
    /// In other words, the position changed because the track is playing.
    /// </summary>
    Playback,

    /// <summary>
    /// Position changed because of user seeking.
    /// </summary>
    UserRequestedSeeked
}

internal delegate void WriteSamples(ReadOnlySpan<byte> samples, int sampleRate, int channels,
    WaitForConsumptionOption waitForConsumptionOption);

/// <summary>
/// A struct that represents the option to wait for the samples to be consumed.
/// </summary>
/// <param name="Wait">
/// If true, the method will wait for the samples to be consumed before returning.
/// </param>
/// <param name="FractionOfSamplesToWaitFor">
/// A value between 0 and 1 that represents the fraction of samples to wait for.
///
/// If 0, the method will return immediately.
/// If 1, the method will wait for all samples to be consumed before returning.
/// </param>
internal readonly record struct WaitForConsumptionOption(bool Wait, float FractionOfSamplesToWaitFor);