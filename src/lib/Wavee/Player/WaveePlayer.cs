using System.Reactive.Linq;
using System.Threading.Channels;
using LanguageExt.Common;
using LanguageExt.UnsafeValueAccess;
using NAudio.Wave;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;
using Wavee.Player.Context;
using Wavee.Player.Playback;

namespace Wavee.Player;

internal sealed class WaveePlayer<RT> : IWaveePlayer
    where RT : struct, HasAudioOutput<RT>
{
    private readonly RT _runtime;
    private readonly Ref<Option<IPlayContext>> _playContext = Ref(Option<IPlayContext>.None);
    private readonly Ref<Option<CurrentPositionRecord>> _currentPosition = Ref(Option<CurrentPositionRecord>.None);
    private readonly Ref<Option<PlayingItem>> _currentItem = Ref(Option<PlayingItem>.None);
    private readonly Ref<bool> _isPaused = Ref(false);
    private readonly Ref<bool> _playbackIsHappening = Ref(false);
    private readonly Ref<RepeatState> _repeatState = Ref(RepeatState.None);
    private readonly Ref<bool> _shuffle = Ref(false);
    private readonly Channel<IInternalPlaybackCommand> _internalCommandChannel;
    public WaveePlayer(RT runtime)
    {
        _internalCommandChannel = Channel.CreateUnbounded<IInternalPlaybackCommand>();
        _runtime = runtime;
    }
    public async Task<Unit> Command(IWaveePlayerCommand command)
    {
        switch (command)
        {
            case PlayContextCommand playContextCmd:
                var run = await HandlePlayContext(
                    _internalCommandChannel.Reader,
                    playContextCmd,
                    _playContext,
                    _currentPosition,
                    _currentItem,
                    _isPaused,
                    _playbackIsHappening,
                    playContextCmd.StartFrom,
                    playContextCmd.StartPlayback,
                    _shuffle.Value,
                    OnPlaybackEnded).Run(_runtime);
                return Unit.Default;
        }

        return unit;
    }

    public Option<IPlayContext> PlayContext => _playContext.Value;


    private void OnPlaybackEnded(TimeSpan aTimeSpan)
    {
        throw new NotImplementedException();
    }

    public bool PlaybackIsHappening => _playbackIsHappening.Value;
    public bool IsPaused => _isPaused.Value;
    public bool IsShuffling => _shuffle.Value;
    public RepeatState RepeatState => _repeatState.Value;
    public Option<TimeSpan> CurrentPosition => _currentPosition.Value.Map(x => x.Position);
    public Option<PlayingItem> CurrentItem => _currentItem.Value;
    public IObservable<Option<IPlayContext>> PlayContextChanged => _playContext.OnChange();

    public IObservable<Option<PlayingItem>> CurrentItemChanged => _currentItem.OnChange();
    public IObservable<Option<TimeSpan>> CurrentPositionChanged => _currentPosition.OnChange()
        .Where(x => x.Match(Some: f => !f.IsIntermediate, None: () => true))
        .Select(c => c.Map(f => f.Position));

    public IObservable<bool> IsPausedChanged => _isPaused.OnChange();
    public IObservable<bool> PlaybackIsHappeningChanged => _playbackIsHappening.OnChange();
    public IObservable<bool> IsShufflingChanged => _shuffle.OnChange();
    public IObservable<RepeatState> RepeatStateChanged => _repeatState.OnChange();


    private static Aff<RT, Unit> HandlePlayContext(
        ChannelReader<IInternalPlaybackCommand> commander,
        PlayContextCommand playContextCmd,
        Ref<Option<IPlayContext>> playContext,
        Ref<Option<CurrentPositionRecord>> currentPosition,
        Ref<Option<PlayingItem>> currentItem,
        Ref<bool> isPaused,
        Ref<bool> playbackIsHappening,
        Option<TimeSpan> startAt,
        Option<bool> start,
        bool shuffle,
        Action<TimeSpan> onPlaybackEnded) =>
        from index in GetIndex(playContextCmd, shuffle)
        from streamAndIndex in playContextCmd.Context.GetStreamAt(index).ToAff()
        let _ = atomic(() => playContext.Swap(_ => Some(playContextCmd.Context)))
        let __ = atomic(() => currentItem.Swap(_ => Some(new PlayingItem(streamAndIndex.Stream.Item, PlaybackReasonType.Context, streamAndIndex.AbsoluteIndex))))
        from ___ in StartPlayback(streamAndIndex.Stream, currentPosition, isPaused,
            playbackIsHappening,
            startAt,
            start,
            commander,
            onPlaybackEnded)
        select unit;

    private static Eff<RT, Unit> StartPlayback(IPlaybackStream metadataStream,
        Ref<Option<CurrentPositionRecord>> currentPosition,
        Ref<bool> isPaused,
        Ref<bool> playbackIsHappening,
        Option<TimeSpan> startAt,
        Option<bool> start,
        ChannelReader<IInternalPlaybackCommand> commandChannel,
        Action<TimeSpan> onPlaybackEnded)
    {
        //first we need to match a decoder
        var audioStream = metadataStream.AsStream();

        return
            from decoder in audioStream.OpenAudioDecoder()
            from _ in Eff<RT, Unit>((rt) =>
            {
                Task.Run(async () =>
                {
                    bool initial = true;
                    while (true)
                    {
                        if (commandChannel.TryRead(out var command))
                        {
                            //pause/seek/resume etc
                            //TODO:
                        }

                        if (startAt.IsSome)
                        {
                            decoder.CurrentTime = startAt.ValueUnsafe();
                            startAt = None;
                        }

                        if (start.IsSome)
                        {
                            var startVal = start.ValueUnsafe();
                            atomic(() => isPaused.Swap(_ => !startVal));
                            start = None;
                            if (startVal)
                            {
                                AudioOutput<RT>.Start().Run(rt);
                            }
                            else
                            {
                                AudioOutput<RT>.Stop().Run(rt);
                            }
                        }
                        else if(initial)
                        {
                            AudioOutput<RT>.Start().Run(rt);
                            initial = false;
                        }
                        const uint bufferSize = 4096 * 2;
                        Memory<byte> buffer = new byte[bufferSize];
                        var sample = decoder.Read(buffer.Span);
                        if (sample == 0)
                        {
                            //end of stream
                            onPlaybackEnded(decoder.CurrentTime);
                            return;
                        }

                        //TODO: normalisation/equalizer etc

                        var wrote = await AudioOutput<RT>.Write(buffer)
                            .Run(rt);
                        atomic(() =>
                        {
                            currentPosition.Swap(_ => Some(new CurrentPositionRecord(decoder.CurrentTime, true)));
                            playbackIsHappening.Swap(_ => true);
                        });
                    }
                });
                return unit;
            })
            select unit;
        // return Eff((rt) =>
        // {
        //     return 
        // });
    }

    private static Eff<RT, Either<Shuffle, Option<int>>> GetIndex(PlayContextCommand playContextCmd, bool shuffle)
    {
        if (playContextCmd.IndexInContext.IsSome)
        {
            return SuccessEff(Either<Shuffle, Option<int>>.Right(Some(playContextCmd.IndexInContext.ValueUnsafe())));
        }

        if (shuffle)
        {
            return SuccessEff(Either<Shuffle, Option<int>>.Left(new Shuffle()));
        }

        return SuccessEff(Either<Shuffle, Option<int>>.Right(None));
    }
}

internal interface IInternalPlaybackCommand
{
}

public enum RepeatState
{
    None,
    RepeatOne,
    RepeatAll
}

internal readonly record struct CurrentPositionRecord(TimeSpan Position, bool IsIntermediate);