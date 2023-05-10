using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using LanguageExt.UnsafeValueAccess;
using NAudio.Wave;
using Wavee.Infrastructure.Live;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;
using Wavee.Player.Playback;

// ReSharper disable HeapView.BoxingAllocation

[assembly: InternalsVisibleTo("Wavee.Spotify.Playback")]
[assembly: InternalsVisibleTo("Wavee.Spotify.Remote")]

namespace Wavee.Player;

public static class WaveePlayer
{
    static WaveePlayer()
    {
        WaveePlayerRuntime<WaveeRuntime>.Runtime = WaveeCore.Runtime;
    }

    public static IDisposable RegisterStateChange(Action<IWaveePlayerState> stateChanged)
        => WaveePlayerRuntime<WaveeRuntime>.State.OnChange().Subscribe(stateChanged);

    public static async ValueTask<IDisposable> Play(IAudioStream stream,
        Option<TimeSpan> from,
        Option<bool> isPaused,
        Action<IWaveePlayerState> stateChanged,
        CancellationToken ct = default)
    {
        var stateChangedObserver =
            WaveePlayerRuntime<WaveeRuntime>.State.OnChange()
                .Subscribe(stateChanged);
        var aff = WaveePlayerRuntime<WaveeRuntime>.Play(stream, from, isPaused, ct);
        var playResult = await aff.Run(WaveeCore.Runtime);
        playResult.ThrowIfFail();
        return stateChangedObserver;
    }

    public static async ValueTask Pause()
    {
        var aff = WaveePlayerRuntime<WaveeRuntime>.Pause();
        var pauseResult = await aff.Run(WaveeCore.Runtime);
        pauseResult.ThrowIfFail();
    }

    public static async ValueTask Seek(TimeSpan to)
    {
        var aff = WaveePlayerRuntime<WaveeRuntime>.Seek(to);
        var seekResult = await aff.Run(WaveeCore.Runtime);
        seekResult.ThrowIfFail();
    }

    public static async ValueTask Resume()
    {
        var aff = WaveePlayerRuntime<WaveeRuntime>.Resume();
        var resumeResult = await aff.Run(WaveeCore.Runtime);
        resumeResult.ThrowIfFail();
    }
}

public interface IAudioStream
{
    IPlaybackItem Item { get; }
    Stream AsStream();
}

internal static class WaveePlayerRuntime<RT> where RT : struct, HasAudioOutput<RT>
{
    private static ChannelWriter<IInternalPlaybackCommand> _commandWriter;
    public static RT Runtime { get; set; }

    public static Ref<IWaveePlayerState> State = Ref((IWaveePlayerState)new InvalidPlayerState(DateTimeOffset.UtcNow));

    static WaveePlayerRuntime()
    {
        var channel = Channel.CreateUnbounded<IInternalPlaybackCommand>();
        _commandWriter = channel.Writer;

        var commandReader = channel.Reader;
        Task.Factory.StartNew(async () =>
        {
            await foreach (var command in commandReader.ReadAllAsync())
            {
                switch (command)
                {
                    case InternalStopCommand:
                        AudioOutput<RT>.Stop().Run(Runtime);
                        AudioOutput<RT>.DiscardBuffer().Run(Runtime);
                        atomic(() => State.Swap(f =>
                        {
                            switch (f)
                            {
                                case IWaveeInPlaybackState playbackState:
                                    playbackState.Decoder.Dispose();
                                    playbackState.Stream.AsStream().Dispose();
                                    return new StoppedPlayerState(DateTimeOffset.UtcNow,
                                        Item: Some(playbackState.Stream.Item));
                                case StoppedPlayerState stoppedState:
                                    return stoppedState with { Since = DateTimeOffset.UtcNow };
                                default:
                                    return new StoppedPlayerState(DateTimeOffset.UtcNow, None);
                            }
                        }));
                        break;
                    case InternalResumeCommand:
                        AudioOutput<RT>.Start().Run(Runtime);
                        atomic(() => State.Swap(f =>
                        {
                            switch (f)
                            {
                                case WaveePausedState paused:
                                    return paused.ToPlaying();
                                default:
                                    return f;
                            }
                        }));
                        break;
                    case InternalPauseCommand:
                        AudioOutput<RT>.Stop().Run(Runtime);
                        atomic(() => State.Swap(f =>
                        {
                            switch (f)
                            {
                                case WaveePlayingState playing:
                                    return playing.ToPaused();
                                default:
                                    return f;
                            }
                        }));
                        break;
                    case InternalSeekCommand to:
                        atomic(() => State.Swap(f =>
                        {
                            switch (f)
                            {
                                case WaveePausedState paused:
                                    paused.Decoder.CurrentTime = to.SeekTo;
                                    return paused with
                                    {
                                        PositionAsOfTimestamp = paused.Decoder.CurrentTime,
                                        Since = DateTimeOffset.UtcNow
                                    };
                                case WaveePlayingState playing:
                                    AudioOutput<RT>.DiscardBuffer().Run(Runtime);
                                    playing.Decoder.CurrentTime = to.SeekTo;
                                    return playing with
                                    {
                                        PositionAsOfTimestamp = playing.Decoder.CurrentTime,
                                        Since = DateTimeOffset.UtcNow
                                    };
                                default:
                                    return f;
                            }
                        }));
                        break;
                    case InternalPlayCommand play:
                        //if we are already playing, stop
                        if (State.Value is IWaveeInPlaybackState playing)
                        {
                            AudioOutput<RT>.Stop().Run(Runtime);
                            AudioOutput<RT>.DiscardBuffer().Run(Runtime);
                            playing.Decoder.Dispose();
                            playing.Stream.AsStream().Dispose();
                        }

                        var startAt = play.StartAt;
                        var startPaused = play.StartPaused;
                        var decoderMaybe =
                            play.Stream.AsStream().OpenAudioDecoder(play.Stream.Item.Duration).Run();
                        bool initial = true;
                        await Task.Factory.StartNew(async () =>
                        {
                            do
                            {
                                if (decoderMaybe.IsFail)
                                {
                                    Debugger.Break();
                                }

                                var decoder = decoderMaybe.ThrowIfFail();

                                if (startAt.IsSome)
                                {
                                    decoder.CurrentTime = startAt.ValueUnsafe();
                                    startAt = None;
                                }

                                if (startPaused.IsSome)
                                {
                                    var paused = startPaused.ValueUnsafe();
                                    if (paused)
                                    {
                                        AudioOutput<RT>.Stop().Run(Runtime);
                                        atomic(() => State.Swap(_ =>
                                            new WaveePausedState(DateTimeOffset.UtcNow, play.Stream,
                                                decoder.CurrentTime)
                                            {
                                                Decoder = decoder
                                            }));
                                    }
                                    else
                                    {
                                        AudioOutput<RT>.Start().Run(Runtime);
                                        atomic(() => State.Swap(_ =>
                                            new WaveePlayingState(DateTimeOffset.UtcNow, play.Stream,
                                                decoder.CurrentTime)
                                            {
                                                Decoder = decoder
                                            }));
                                    }

                                    startPaused = Option<bool>.None;
                                }
                                else if (initial)
                                {
                                    AudioOutput<RT>.Start().Run(Runtime);

                                    atomic(() =>
                                        State.Swap(_ =>
                                            new WaveePlayingState(DateTimeOffset.UtcNow, play.Stream,
                                                decoder.CurrentTime)
                                            {
                                                Decoder = decoder
                                            }));
                                    initial = false;
                                }

                                const uint bufferSize = 4096;
                                Memory<byte> buffer = new byte[bufferSize];
                                var sample = decoder.Read(buffer.Span);
                                if (sample == 0)
                                {
                                    //end of stream
                                    return;
                                }

                                var wrote = await AudioOutput<RT>.Write(buffer)
                                    .Run(Runtime);
                            } while (State.Value is IWaveeInPlaybackState);
                        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                        break;
                }
            }
        });
    }

    public static Aff<RT, Unit> Play(IAudioStream stream,
        Option<TimeSpan> startAt,
        Option<bool> startPaused,
        CancellationToken ct = default) =>
        from _ in Aff(async () =>
        {
            await _commandWriter.WriteAsync(new InternalPlayCommand(stream, startAt, startPaused), ct);
            return unit;
        })
        select unit;

    public static Aff<RT, Unit> Seek(TimeSpan to) =>
        from _ in Aff(async () =>
        {
            await _commandWriter.WriteAsync(new InternalSeekCommand(to));
            return unit;
        })
        select unit;

    public static Aff<RT, Unit> Pause() =>
        from _ in Aff(async () =>
        {
            await _commandWriter.WriteAsync(new InternalPauseCommand());
            return unit;
        })
        select unit;

    public static Aff<RT, Unit> Resume() =>
        from _ in Aff(async () =>
        {
            await _commandWriter.WriteAsync(new InternalResumeCommand());
            return unit;
        })
        select unit;

    private interface IInternalPlaybackCommand
    {
    }

    private readonly record struct InternalStopCommand : IInternalPlaybackCommand;

    private readonly record struct InternalPauseCommand : IInternalPlaybackCommand;

    private readonly record struct InternalResumeCommand : IInternalPlaybackCommand;

    private readonly record struct InternalSeekCommand(TimeSpan SeekTo) : IInternalPlaybackCommand;

    private readonly record struct InternalPlayCommand
        (IAudioStream Stream, Option<TimeSpan> StartAt, Option<bool> StartPaused) : IInternalPlaybackCommand;
}

public interface IWaveePlayerState
{
}

public interface IWaveeInPlaybackState : IWaveePlayerState
{
    DateTimeOffset Since { get; init; }
    IAudioStream Stream { get; }
    TimeSpan PositionAsOfTimestamp { get; init; }
    internal WaveStream Decoder { get; }
    TimeSpan Position { get; }
}

public readonly record struct WaveePausedState
    (DateTimeOffset Since, IAudioStream Stream, TimeSpan PositionAsOfTimestamp) : IWaveeInPlaybackState
{
    public required WaveStream Decoder { get; init; }

    public TimeSpan Position => Decoder.CurrentTime;

    public WaveePlayingState ToPlaying()
    {
        return new(DateTimeOffset.UtcNow, Stream, Decoder.CurrentTime)
        {
            Decoder = Decoder
        };
    }
}

public readonly record struct WaveePlayingState(DateTimeOffset Since, IAudioStream Stream,
    TimeSpan PositionAsOfTimestamp) : IWaveeInPlaybackState
{
    public TimeSpan Position => Decoder.CurrentTime;

    public WaveePausedState ToPaused()
    {
        return new(DateTimeOffset.UtcNow, Stream, Decoder.CurrentTime)
        {
            Decoder = Decoder
        };
    }

    public required WaveStream Decoder { get; init; }
}

public readonly record struct InvalidPlayerState(DateTimeOffset Since) : IWaveePlayerState;

public readonly record struct StoppedPlayerState(DateTimeOffset Since, Option<IPlaybackItem> Item) : IWaveePlayerState;