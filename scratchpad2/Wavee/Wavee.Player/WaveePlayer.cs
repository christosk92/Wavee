using System.Reactive.Linq;
using System.Threading.Channels;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Serilog;
using Wavee.Player.Command;
using Wavee.Player.Ctx;
using Wavee.Player.State;
using static LanguageExt.Prelude;

namespace Wavee.Player;

public sealed class WaveePlayer : IWaveePlayer
{
    private readonly ChannelWriter<IWaveePlaybackCommand> _writer;

    private readonly Ref<Option<WaveePlayerState>> _state = Ref(Option<WaveePlayerState>.None);

    public WaveePlayer()
    {
        var main = Channel.CreateUnbounded<IWaveePlaybackCommand>();
        _writer = main.Writer;
        var internalPlayer = new PlayerInternal();
        Task.Factory.StartNew(async () =>
        {
            try
            {
                await foreach (var command in main.Reader.ReadAllAsync())
                {
                    await OnPlaybackEvent(command, internalPlayer);
                }
            }
            finally
            {
                main.Writer.Complete();
            }
        });
    }

    public IObservable<Option<WaveePlayerState>> CreateListener() => _state.OnChange().StartWith(_state);
    public Option<WaveePlayerState> CurrentState => _state.Value;

    public ValueTask Play(WaveeContext ctx, int idx, Option<TimeSpan> startFrom, bool startPaused,
        Option<bool> shuffling, Option<RepeatState> repeatState)
    {
        var command = IWaveePlaybackCommand.Play(
            Context: ctx,
            Index: idx,
            StartFrom: startFrom,
            StartPaused: startPaused,
            Shuffling: shuffling,
            RepeatState: repeatState
        );
        return _writer.WriteAsync(command);
    }

    private async Task OnPlaybackEvent(IWaveePlaybackCommand command, PlayerInternal player)
    {
        try
        {
            switch (command)
            {
                case WaveePlaybackPlayCommand play:
                {
                    var currentStateOption = _state.Value;
                    if (currentStateOption.IsNone)
                    {
                        currentStateOption = new WaveePlayerState();
                    }

                    var currentState = currentStateOption.ValueUnsafe();
                    

                    var track = play.Context.FutureTracks.ElementAtOrDefault(play.Index) ??
                                play.Context.FutureTracks.ElementAtOrDefault(0);
                    var nextState = currentState.PlayContext(play.Context,
                        play.Index,
                        play.StartFrom,
                        play.Shuffling,
                        play.RepeatState,
                        track);

                    atomic(() => _state.Swap(_ => nextState));

                    if (track is null)
                    {
                        //set permanent end state
                        atomic(() => _state.Swap(_ => nextState.PermanentEnd()));
                        return;
                    }

                    var stream = await track.Factory(CancellationToken.None);
                    atomic(() => _state.Swap(_ => nextState.Playing(stream, Guid.NewGuid().ToString())));
                    player.Play(stream, track);
                    if (play.StartPaused)
                    {
                        player.Pause();
                    }
                    else
                    {
                        player.Resume();
                    }

                    break;
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Error while handling playback event. Going to next track.");
        }
    }
}

internal sealed class PlayerInternal
{
    public void Play(WaveeTrack stream, FutureWaveeTrack futureWaveeTrack)
    {
        
    }

    public void Pause()
    {
        throw new NotImplementedException();
    }

    public void Resume()
    {
        throw new NotImplementedException();
    }
}

public interface IWaveePlayer
{
    IObservable<Option<WaveePlayerState>> CreateListener();
    Option<WaveePlayerState> CurrentState { get; }

    ValueTask Play(WaveeContext ctx, int idx, Option<TimeSpan> startFrom, bool startPaused, Option<bool> shuffling,
        Option<RepeatState> repeatState);
}