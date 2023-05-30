using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Playback;
using Wavee.Core.Player.InternalCommanding;
using Wavee.Core.Player.PlaybackStates;
using static LanguageExt.Prelude;


[assembly: InternalsVisibleTo("Wavee.AudioOutput.NAudio")]

namespace Wavee.Core.Player;

public sealed class WaveePlayer
{
    private static ChannelWriter<IInternalPlayerCommand> _commander;
    private static Ref<WaveePlayerState> State { get; } = Ref(WaveePlayerState.Default);
    internal static Option<IWaveeAudioOutput> Output { get; set; }

    static WaveePlayer()
    {
        var channel = Channel.CreateUnbounded<IInternalPlayerCommand>();
        _commander = channel.Writer;

        Task.Factory.StartNew(async () =>
        {
            await foreach (var command in channel.Reader.ReadAllAsync())
            {
                try
                {
                    await (command switch
                    {
                        PlayContextCommand playContextCommand => PlayContext(playContextCommand),
                        SkipNextCommand _ => DoSkipNext().AsTask(),
                        PauseCommand _ => HandlePause().AsTask(),
                        ResumeCommand _ => HandleResume().AsTask(),
                        SeekCommand seekCommand => HandleSeek(seekCommand.To).AsTask(),
                        _ => throw new NotImplementedException()
                    });
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }, TaskCreationOptions.LongRunning);
    }

    private static ValueTask HandleSeek(TimeSpan seekCommandTo)
    {
        Output.IfSome(x => x.Pause());
        atomic(() => State.Swap(f => f with
        {
            PlaybackState = f.PlaybackState switch
            {
                WaveePlaybackPlayingState playing => playing with
                {
                    Decoder = playing.Decoder.Seek(seekCommandTo)
                },
                WaveePlaybackLoadingState loading => loading with
                {
                    StartAt = seekCommandTo
                },
            }
        }));
        Output.IfSome(x => x.DiscardBuffer());
        Output.IfSome(x => x.Resume());
        return ValueTask.CompletedTask;
    }

    private static ValueTask HandleResume()
    {
        Output.IfSome(x => x.Resume());
        atomic(() => State.Swap(f => f with
        {
            PlaybackState = f.PlaybackState switch
            {
                WaveePlaybackPlayingState playing => playing with
                {
                    Paused = false
                },
                WaveePlaybackLoadingState loading => loading with
                {
                    StartPaused = false
                },
                _ => f.PlaybackState
            }
        }));
        return ValueTask.CompletedTask;
    }

    private static ValueTask HandlePause()
    {
        Output.IfSome(x => x.Pause());
        atomic(() => State.Swap(f => f with
        {
            PlaybackState = f.PlaybackState switch
            {
                WaveePlaybackPlayingState playing => playing with
                {
                    Paused = true
                },
                WaveePlaybackLoadingState loading => loading with
                {
                    StartPaused = true
                },
                _ => f.PlaybackState
            }
        }));
        return ValueTask.CompletedTask;
    }

    public static ValueTask PlayContext(WaveeContext context, Option<int> startFromIndexInContext)
    {
        return _commander.WriteAsync(new PlayContextCommand(context, startFromIndexInContext));
    }

    private static async Task DoSkipNext()
    {
        var state = State.Value;
        bool crossfading = state.PlaybackState is WaveePlaybackEndedState { CrossfadingIntoNextTrack: true };
        if (state.PlaybackState is WaveePlaybackEndedState { CrossfadingIntoNextTrack: false } previousEnded)
        {
            previousEnded.Stream.Dispose();
            previousEnded.Decoder.Dispose();
        }

        if (state.PlaybackState is WaveePlaybackPlayingState playing)
        {
            playing.Stream.Dispose();
            playing.Decoder.Dispose();
        }

        var newState = atomic(() => State.Swap(f => f.SkipNext(crossfading)));

        if (newState.PlaybackState is PermanentEndOfContextPlaybackState)
        {
        }

        if (newState.PlaybackState is not WaveePlaybackLoadingState loadingState)
        {
            //we did nothing. what the hell!!
            return;
        }

        Output.IfSome(x => x.Resume());
        await PlayTrack(loadingState);
    }

    private static Task PlayContext(PlayContextCommand playContextCommand)
    {
        var state = atomic(() =>
            State.Swap(f => f.FromNewContext(playContextCommand.Context, playContextCommand.StartFromIndexInContext)));

        if (state.PlaybackState is not WaveePlaybackLoadingState loadingState)
        {
            //we did nothing. what the hell!!
            return Task.CompletedTask;
        }

        Output.IfSome(x => x.Resume());

        return PlayTrack(loadingState);
    }

    private static async Task PlayTrack(WaveePlaybackLoadingState loadingState)
    {
        var audioStream = await loadingState.Stream;
        var decoder = Output.Match(
            Some: output => output.OpenDecoder(audioStream.AsStream(), audioStream.Track.Duration),
            None: () => throw new InvalidOperationException("No output device selected"));

        var state = atomic(() => State.Swap(f => f.FromPlayingTrack(loadingState, decoder, audioStream)));

        if (state.PlaybackState is not WaveePlaybackPlayingState playingState)
        {
            //we did nothing. what the hell!!
            return;
        }

        //start reading samples
        _ = Task.Run(async () =>
        {
            var crossfading = ReadUntilDoneOrStopped(decoder);

            atomic(() => State.Swap(f => f.FromEndedTrack(crossfading)));

            await _commander.WriteAsync(new SkipNextCommand());
        });
    }

    private static bool ReadUntilDoneOrStopped(IAudioDecoder decoder)
    {
        bool startedcrossfading = false;
        try
        {
            while (true)
            {
                var buffer = new float[4096];
                decoder.ReadSamples(buffer);
                if (buffer.Length == 0 || decoder.Ended)
                {
                    //we're done
                    break;
                }
                //mutate (Normalisation/equalizer)
                //TODO

                var samples = MemoryMarshal.Cast<float, byte>(buffer.AsSpan());

                //TODO: Check if null, then wait.
                var potentialOutput = Output.ValueUnsafe();
                potentialOutput.WriteSamples(samples);
            }
        }
        catch (Exception)
        {
            //for whatever reason (disposing etc)
            //Just stop
        }

        return startedcrossfading;
    }

    public static IObservable<WaveePlayerState> StateChanged => State.OnChange();

    public static Option<TimeSpan> Position => State.Value.PlaybackState switch
    {
        WaveePlaybackLoadingState loadingState => loadingState.StartAt,
        WaveePlaybackPlayingState playingState => playingState.Decoder.DecodePosition,
        _ => None
    };

    public static async ValueTask<Unit> SkipNext()
    {
        if (State.Value.PlaybackState is WaveePlaybackPlayingState p)
        {
            p.Decoder.Dispose();
            p.Stream.Dispose();
        }
        else if (State.Value.PlaybackState is WaveePlaybackLoadingState l)
        {
            l.Stream.Dispose();
        }
        else
        {
            await _commander.WriteAsync(new SkipNextCommand());
        }

        return unit;
    }

    public static async ValueTask<Unit> Resume()
    {
        await _commander.WriteAsync(new ResumeCommand());
        return unit;
    }

    public static async ValueTask<Unit> Seek(TimeSpan ts, OptionNone none)
    {
        await _commander.WriteAsync(new SeekCommand(ts));
        return unit;
    }

    public static async ValueTask<Unit> Pause()
    {
        await _commander.WriteAsync(new PauseCommand());
        return unit;
    }
}