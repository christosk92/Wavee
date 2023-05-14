using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using LanguageExt;
using Wavee.Core.Enums;
using Wavee.Core.Infrastructure.Live;
using Wavee.Core.Infrastructure.Sys.IO;
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

    public static Option<TimeSpan> Position =>
        AudioOutput<WaveeRuntime>.Position.Run(Runtime).ThrowIfFail();

    public static void SkipNext(bool immediately)
    {
        _commandChannelWriter.TryWrite(new SkipNextCommand(immediately));
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

    private static async void StartMainLoop(ChannelReader<IInternalPlayerCommand> channelReader)
    {
        await Task.Factory.StartNew(async () =>
        {
            await foreach (var command in channelReader.ReadAllAsync())
            {
                
            }
        });
    }


    public static IObservable<WaveePlayerState> StateChanged => _state.OnChange().StartWith(State);

    private interface IInternalPlayerCommand
    {
    }

    private readonly record struct PreloadNextCommand : IInternalPlayerCommand;

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

    private readonly record struct SeekCommand(DateTimeOffset Since, TimeSpan Position) : IInternalPlayerCommand;
}