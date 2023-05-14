using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using LanguageExt;
using LanguageExt.Effects.Traits;
using Wavee.Core.Enums;
using Wavee.Core.Id;
using Wavee.Core.Infrastructure.Live;
using Wavee.Core.Infrastructure.Sys;
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

    public static WaveePlayerState State => _state.Value;

    private static ChannelWriter<IInternalPlayerCommand> _commandChannelWriter;

    private static WaveeRuntime Runtime => WaveeCore.Runtime;

    static WaveePlayer()
    {
        var channel = Channel.CreateUnbounded<IInternalPlayerCommand>();
        _commandChannelWriter = channel.Writer;
        StartMainLoop(channel.Reader);
    }

    public static void SkipNext(bool immediately)
    {
        _commandChannelWriter.TryWrite(new SkipNextCommand(immediately));
    }

    public static void PlayContext(WaveeContext context, TimeSpan startFrom, int index, bool startPaused)
    {
        _commandChannelWriter.TryWrite(new PlayContextCommand(context, startFrom, index, startPaused));
    }

    public static void Pause()
    {
        _commandChannelWriter.TryWrite(PauseCommand.Default);
    }

    public static void Resume()
    {
        _commandChannelWriter.TryWrite(ResumeCommand.Default);
    }

    public static void Seek(TimeSpan position)
    {
        _commandChannelWriter.TryWrite(new SeekCommand(position));
    }

    private static async void StartMainLoop(ChannelReader<IInternalPlayerCommand> channelReader)
    {
        await Task.Factory.StartNew(async () =>
        {
            await foreach (var command in channelReader.ReadAllAsync())
            {
                switch (command)
                {
                    case SeekCommand seek:
                        AudioOutput<WaveeRuntime>.Seek(seek.Position).Run(Runtime).ThrowIfFail();
                        break;
                    case SkipNextCommand skipNext:
                        if (!skipNext.Immediately)
                        {
                            await _playerLock.WaitAsync();
                        }

                        var swappedTo = atomic(() => _state.Swap(f => f.SkipNext(skipNext.Immediately)));
                        _playerLock.Release();
                        if (swappedTo.State is WaveeLoadingState)
                            _playerReady.Set();
                        GC.Collect();
                        break;
                    case PauseCommand:
                        var pos = AudioOutput<WaveeRuntime>.Pause().Run(Runtime).ThrowIfFail();
                        atomic(() => _state.Swap(f =>
                        {
                            return f with
                            {
                                State = f.State switch
                                {
                                    WaveeLoadingState loadingState => loadingState with { StartPaused = true },
                                    WaveePlayingState playingState => playingState.ToPausedState(pos),
                                    _ => f.State
                                }
                            };
                        }));
                        break;
                    case ResumeCommand:
                        AudioOutput<WaveeRuntime>.Start().Run(Runtime);
                        atomic(() => _state.Swap(f =>
                        {
                            return f with
                            {
                                State = f.State switch
                                {
                                    WaveeLoadingState loadingState => loadingState with { StartPaused = true },
                                    WaveePausedState pausedState => pausedState.ToPlayingState(),
                                    _ => f.State
                                }
                            };
                        }));
                        break;
                    case PlayContextCommand playContext:
                        //end track if playing
                        atomic(() => _state.Swap(f => f.PlayContext(
                            context: playContext.Context,
                            startFrom: playContext.StartFrom,
                            index: playContext.Index,
                            startPaused: playContext.StartPaused
                        )));
                        //wait for player to be ready
                        await _playerLock.WaitAsync();
                        _playerReady.Set();
                        _playerLock.Release();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(command));
                }
            }
        });


        await Task.Factory.StartNew(async () =>
        {
            while (true)
            {
                _playerReady.WaitOne();
                if (_state.Value.State is WaveeNothingState or WaveeEndedState)
                {
                    _playerReady.Reset();
                    continue;
                }

                Option<TimeSpan> startFrom = None;
                bool closeOtherStreams = true;
                bool startPaused = false;
                if (_state.Value.State is WaveeLoadingState loadingState)
                {
                    //get loader
                    var str = await loadingState.Stream;
                    atomic(() => _state.Swap(f => f with
                    {
                        State = loadingState.ToPlayingOrPaused(str)
                    }));
                    startFrom = loadingState.StartFrom;
                    startPaused = loadingState.StartPaused;
                    closeOtherStreams = loadingState.CloseOtherStreams;
                }

                var state = _state.Value.State;
                if (state is not IWaveeInPlaybackState inPlaybackState)
                {
                    _playerReady.Reset();
                    Log<WaveeRuntime>.logInfo("Player is not in playback state, resetting")
                        .Run(Runtime);
                    continue;
                }

                var stream = inPlaybackState.Stream.AsStream();
                //put stream into audio output and wait for it to finish
                await _playerLock.WaitAsync();

                void OnPositionChanged(TimeSpan obj)
                {
                    //check for crossfades
                }

                var handle = AudioOutput<WaveeRuntime>.PlayStream(stream, OnPositionChanged, closeOtherStreams)
                    .Run(Runtime).ThrowIfFail();
                if (startPaused)
                {
                    AudioOutput<WaveeRuntime>.Pause().Run(Runtime);
                }
                else
                {
                    AudioOutput<WaveeRuntime>.Start().Run(Runtime);
                }

                await handle;
                atomic(() => _state.Swap(f => f with
                {
                    State = inPlaybackState switch
                    {
                        WaveePlayingState p => p.ToEndedState(),
                        WaveePausedState p => p.ToEndedState(),
                    }
                }));
                _commandChannelWriter.TryWrite(new SkipNextCommand(false));
                _playerLock.Release();
            }
        });
    }


    public static IObservable<WaveePlayerState> StateChanged => _state.OnChange().StartWith(State);

    private interface IInternalPlayerCommand
    {
    }

    private readonly record struct SkipNextCommand(bool Immediately) : IInternalPlayerCommand;

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

    private readonly record struct SeekCommand(TimeSpan Position) : IInternalPlayerCommand;
}