using System.Diagnostics;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core;
using Wavee.Core.Contracts;
using Wavee.Core.Enums;
using Wavee.Core.Infrastructure.Live;
using Wavee.Core.Infrastructure.Sys.IO;
using Wavee.Core.Infrastructure.Traits;
using Wavee.Player.States;

[assembly: InternalsVisibleTo("Wavee.Core")]

namespace Wavee.Player;

public static class WaveePlayer
{
    private static readonly ManualResetEvent _playerReady = new(false);
    private static readonly SemaphoreSlim _playerLock = new(1, 1);

    private static readonly Ref<WaveePlayerState> _state = Ref(new WaveePlayerState(
        WaveeNothingState.Default,
        Option<WaveeContext>.None,
        RepeatStateType.None,
        false,
        Que<FutureTrack>.Empty
    ));

    private static bool _crossfadeStarted;

    private static Option<(IAudioDecoder Decoder, CrossfadeController Controller)> _crossfadingOutDecoder =
        Option<(IAudioDecoder Decoder, CrossfadeController Controller)>.None;

    public static WaveePlayerState State => _state.Value;

    private static ChannelWriter<IInternalPlayerCommand> _commandChannelWriter;

    private static WaveeRuntime Runtime => WaveeCore.Runtime;

    static WaveePlayer()
    {
        var channel = Channel.CreateUnbounded<IInternalPlayerCommand>();
        _commandChannelWriter = channel.Writer;
        StartMainLoop(channel.Reader);
    }

    private static async void StartMainLoop(ChannelReader<IInternalPlayerCommand> channelReader)
    {
        await Task.Factory.StartNew(async () =>
        {
            await foreach (var command in channelReader.ReadAllAsync())
            {
                try
                {
                    switch (command)
                    {
                        case PlayContextCommand playContextCommand:
                            await PlayContextInternal(playContextCommand);
                            break;
                        case SeekCommand seekCommand:
                            SeekInternal(seekCommand);
                            break;
                        case SkipNextCommand skipNextCommand:
                            SkipNextInternal(skipNextCommand);
                            break;
                    }
                }
                catch (Exception x)
                {
                    Debug.WriteLine(x.ToString());
                }
            }
        }, TaskCreationOptions.LongRunning);
        await Task.Factory.StartNew(async () =>
        {
            while (true)
            {
                _playerReady.WaitOne();
                await DoPlaybackLoop();
            }
        }, TaskCreationOptions.LongRunning);
    }

    #region Playback

    private static async Task DoPlaybackLoop()
    {
        //check if we have a track to play 
        //if the state is loading, wait for it to finish
        if (_state.Value.State is not WaveeLoadingState loadingState)
        {
            return;
        }

        var stream = await loadingState.Stream;
        atomic(() => _state.Swap(f => f with
        {
            State = loadingState.ToPlayingOrPaused(stream)
        }));

        if (State.State is WaveePausedState)
        {
            AudioOutput<WaveeRuntime>.Pause().Run(Runtime).ThrowIfFail();
        }

        if (State.State is WaveePlayingState)
        {
            AudioOutput<WaveeRuntime>.Start().Run(Runtime).ThrowIfFail();
        }

        //start consuming the stream
        try
        {
            var output = (await AudioOutput<WaveeRuntime>.Decode(stream).Run(Runtime)).ThrowIfFail();
            stream.CrossfadeController.IfSome(x =>
            {
                if (loadingState.StartFadeIn)
                    x.FlagCrossFadeIn();
            });
            var crossfading = DoPlayback(output, stream.Track.Duration, stream.CrossfadeController);
            if (crossfading)
            {
                _crossfadingOutDecoder = Some((output, stream.CrossfadeController.ValueUnsafe()));
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            _playerReady.Reset();
            atomic(() => _state.Swap(f => f with
            {
                State = new WaveeEndedState(stream.Track, TimeSpan.Zero, loadingState.IndexInContext,
                    loadingState.Uid,
                    loadingState.FromQueue)
                {
                    Stream = stream
                }
            }));

            //skip next
            _commandChannelWriter.TryWrite(new SkipNextCommand(true, false));
        }
    }

    private static bool DoPlayback(IAudioDecoder output, TimeSpan duration,
        Option<CrossfadeController> crossfadeController)
    {
        //start reading samples, manipulating them 
        //and sending them to the output
        TimeSpan prevPositionAsOf = TimeSpan.Zero;
        DateTimeOffset prevPositionAsOfSince = DateTimeOffset.MinValue;

        using var listener = _state.OnChange()
            .Where(c => c.State is WaveePausedState || c.State is WaveePlayingState)
            .Select(c =>
            {
                static TimeSpan Seek(IAudioDecoder decoder, TimeSpan position)
                {
                    bool success = false;
                    while (!success)
                    {
                        try
                        {
                            decoder.Seek(position);
                            return position;
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e);
                            //go back a bit
                            position -= TimeSpan.FromMilliseconds(50);
                        }
                    }

                    return position;
                }

                switch (c.State)
                {
                    case WaveePlayingState p:
                        if (p.Since != prevPositionAsOfSince && p.PositionAsOfSince != prevPositionAsOf)
                        {
                            //seek
                            prevPositionAsOf = Seek(output, p.PositionAsOfSince);
                            AudioOutput<WaveeRuntime>.DiscardSamples().Run(WaveeCore.Runtime);
                            prevPositionAsOfSince = p.Since;
                        }

                        break;
                    case WaveePausedState p:
                        if (p.Position != prevPositionAsOf)
                        {
                            //seek
                            prevPositionAsOf = Seek(output, p.Position);
                            AudioOutput<WaveeRuntime>.DiscardSamples().Run(WaveeCore.Runtime);
                        }

                        break;
                }

                return unit;
            }).Subscribe();


        static (bool notifiedTrackEnd, bool isCrossfading) Loop(IAudioDecoder output,
            Option<CrossfadeController> crossfadeController, TimeSpan duration)
        {
            bool notifiedTrackEnd = false;
            bool isCrossfading = false;
            while (true)
            {
                try
                {
                    int samples = 1024;
                    var sample = output.ReadSamples(samples);
                    if (sample.Length == 0)
                    {
                        break;
                    }

                    _crossfadeStarted = true;

                    //check for crossfades
                    if (_crossfadingOutDecoder.IsSome)
                    {
                        //compose the crossfade

                        //this stream should be fading in
                        float gain = crossfadeController.Match(
                            controller => controller.GetFactor(output.Position, duration),
                            () => 1f
                        );

                        //output the sample
                        var (fadingoutDecoder, fadingoutCrossfadeController) = _crossfadingOutDecoder.ValueUnsafe();
                        float fadingOutgain = fadingoutCrossfadeController
                            .GetFactor(fadingoutDecoder.Position, fadingoutDecoder.TotalTime);

                        var fadingoutSamples = fadingoutDecoder.ReadSamples(sample.Length);
                        if (fadingoutSamples.Length == 0)
                        {
                            //we're done
                            //dispose the decoder
                            fadingoutDecoder.Dispose();
                            _crossfadingOutDecoder = None;
                            GC.Collect();
                        }
                        else
                        {
                            //mix the samples
                            for (int i = 0; i < fadingoutSamples.Length; i++)
                            {
                                sample[i] = sample[i] * gain + fadingoutSamples[i] * fadingOutgain;
                            }
                        }
                    }


                    //check if we reached the preload/crossfade 
                    crossfadeController.IfSome(x =>
                    {
                        if (x.MaybeFlagCrossFadeOut(output.Position, duration))
                        {
                            _crossfadeStarted = false;
                            //to prevent halting until the next track is loaded, consume the rest of the stream until playback is started
                            Task.Run(() =>
                            {
                                while (_crossfadeStarted == false)
                                {
                                    var s = output.ReadSamples(1024);
                                    var gain = x.GetFactor(output.Position, duration);
                                    for (int i = 0; i < s.Length; i++)
                                    {
                                        s[i] *= gain;
                                    }

                                    AudioOutput<WaveeRuntime>.GetAudioOutput().Run(Runtime).ThrowIfFail()
                                        .WriteSamples(s);
                                }
                            });
                            notifiedTrackEnd = true;
                            _commandChannelWriter.TryWrite(new SkipNextCommand(false, true));
                            //break out, we're done
                            isCrossfading = true;
                        }
                    });

                    //send sample to output
                    AudioOutput<WaveeRuntime>.GetAudioOutput().Run(Runtime).ThrowIfFail().WriteSamples(sample);
                    if (notifiedTrackEnd)
                        break;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    break;
                }
            }

            return (notifiedTrackEnd, isCrossfading);
        }

        var (notify, isCrossfading) = Loop(output, crossfadeController, duration);

        if (!notify)
        {
            listener.Dispose();
            _commandChannelWriter.TryWrite(new SkipNextCommand(false, false));
            //dispose the output
            output.Dispose();
            GC.Collect();
        }

        return isCrossfading;
    }

    #endregion

    #region Public commanding

    public static Option<TimeSpan> Position =>
        State.State switch
        {
            WaveePlayingState playingState => playingState.Position,
            WaveePausedState pausedState => pausedState.Position,
            WaveeEndedState endedState => endedState.Position,
            _ => Option<TimeSpan>.None
        };

    public static void SkipNext(bool immediately)
    {
        _commandChannelWriter.TryWrite(new SkipNextCommand(immediately, false));
    }

    public static void PlayContext(WaveeContext context, TimeSpan startFrom, int index, bool startPaused)
    {
        _commandChannelWriter.TryWrite(new PlayContextCommand(context, startFrom, index, startPaused));
    }

    public static Unit Pause()
    {
        _commandChannelWriter.TryWrite(PauseCommand.Default);
        return unit;
    }

    public static Unit Resume()
    {
        _commandChannelWriter.TryWrite(ResumeCommand.Default);
        return unit;
    }

    public static Unit Seek(TimeSpan position, Option<DateTimeOffset> since)
    {
        _commandChannelWriter.TryWrite(new SeekCommand(since.IfNone(DateTimeOffset.UtcNow), position));
        return unit;
    }

    public static IObservable<WaveePlayerState> StateChanged => _state.OnChange().StartWith(State);

    #endregion

    #region Internal Commanding

    private static void SkipNextInternal(SkipNextCommand skipNextCommand)
    {
        //if we have a playback, end it
        atomic(() => _state.Swap(f =>
        {
            return f with
            {
                State = f.State switch
                {
                    WaveePlayingState p => p.ToEndedState(),
                    WaveePausedState p => p.ToEndedState(),
                    _ => f.State
                }
            };
        }));

        //if we have a context, skip to the next track
        var newState = atomic(() => _state.Swap(f => f.SkipNext(skipNextCommand.MarkForCrossfade)));
        _playerReady.Set();
    }


    private static void SeekInternal(SeekCommand seekCommand)
    {
        atomic(() =>
        {
            _state.Swap(f => f with
            {
                State = f.State switch
                {
                    WaveePlayingState playingState => playingState with
                    {
                        PositionAsOfSince = seekCommand.Position,
                        Since = DateTimeOffset.UtcNow
                    },
                    WaveePausedState pausedState => pausedState with
                    {
                        Position = pausedState.Position
                    },
                    _ => f.State
                }
            });
        });
    }

    private static Task PlayContextInternal(PlayContextCommand playContextCommand)
    {
        atomic(() => _state.Swap(f => f.PlayContext(playContextCommand.Context,
            playContextCommand.Index,
            playContextCommand.StartFrom,
            playContextCommand.StartPaused)));

        _playerReady.Set();
        return Task.CompletedTask;
    }

    #endregion

    #region Structs

    private interface IInternalPlayerCommand
    {
    }

    private readonly record struct PreloadNextCommand : IInternalPlayerCommand;

    private readonly record struct SkipNextCommand(bool Immediately, bool MarkForCrossfade) : IInternalPlayerCommand;

    private readonly record struct PlayContextCommand(WaveeContext Context, Option<TimeSpan> StartFrom,
        Option<int> Index, Option<bool> StartPaused) : IInternalPlayerCommand;

    private readonly record struct ResumeCommand : IInternalPlayerCommand
    {
        public static ResumeCommand Default = new();
    }

    private readonly record struct PauseCommand : IInternalPlayerCommand
    {
        public static PauseCommand Default = new();
    }

    private readonly record struct SeekCommand(DateTimeOffset Since, TimeSpan Position) : IInternalPlayerCommand;

    #endregion
}