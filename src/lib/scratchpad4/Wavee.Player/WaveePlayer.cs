using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Enums;
using Wavee.Core.Id;
using Wavee.Player.States;

namespace Wavee.Player;

public readonly record struct WaveePlayerState(
    IWaveePlaybackState State,
    Option<WaveeContext> Context,
    RepeatStateType RepeatState,
    bool IsShuffling,
    Que<FutureTrack> Queue)
{
    public WaveePlayerState RemoveFromQueue(int index)
    {
        var i = 0;
        var newQueue = new Que<FutureTrack>(Queue.Filter(x =>
        {
            if (i == index)
            {
                return false;
            }

            i++;
            return true;
        }));
        return this with
        {
            Queue = newQueue
        };
    }

    public WaveePlayerState AddToQueue(FutureTrack uri)
    {
        return this with
        {
            Queue = Queue.Enqueue(uri)
        };
    }

    public WaveePlayerState Shuffle(bool shuffle)
    {
        return this with
        {
            IsShuffling = shuffle
        };
    }

    public WaveePlayerState SetRepeatState(RepeatStateType repeatState)
    {
        return this with
        {
            RepeatState = repeatState
        };
    }

    public WaveePlayerState SetContext(
        WaveeContext context,
        Option<int> index,
        Option<TimeSpan> startFrom, Option<bool> startPaused)
    {
        var ind = index.IfNone(0);
        return this with
        {
            Context = Some(context),
            State = new WaveeLoadingState(
                IndexInContext: ind,
                TrackId: context.FutureTracks.ElementAt(ind).Id,
                FromQueue: false,
                StartFrom: startFrom.IfNone(TimeSpan.Zero),
                StartPaused: startPaused.IfNone(false))
            {
                Stream = context.FutureTracks.ElementAt(ind).StreamFuture()
            },
            IsShuffling = IsShuffling,
            RepeatState = RepeatState,
            Queue = Queue
        };
    }

    public WaveePlayerState SkipNext()
    {
        //check if we have a repeat state
        //if we do, check if we are repeating a track
        if (RepeatState is RepeatStateType.RepeatTrack)
        {
            //just return the current state with the same track
            return this with
            {
                State = State switch
                {
                    WaveeLoadingState loadingState => loadingState with
                    {
                        StartFrom = TimeSpan.Zero,
                        StartPaused = false
                    },
                    WaveePlayingState playingState => playingState with
                    {
                        Position = TimeSpan.Zero
                    },
                    WaveePausedState pausedState => pausedState.ToPlayingState() with
                    {
                        Position = TimeSpan.Zero
                    },
                    WaveeEndedState endedState => endedState.ToPlayingState() with
                    {
                        Position = TimeSpan.Zero
                    },
                    _ => State
                }
            };
        }

        //no repeat state, or we are repeating the context
        //but first lets check if we have an item in a queue
        if (Queue.Count > 0)
        {
            //we have a queue, so lets remove the first item from the queue
            var item = Queue.Head();
            var newQueue = Queue.Dequeue();
            var indexInContext = State switch
            {
                WaveeLoadingState loadingState => loadingState.IndexInContext,
                WaveePlayingState playingState => playingState.IndexInContext,
                WaveePausedState pausedState => pausedState.IndexInContext,
                WaveeEndedState endedState => endedState.IndexInContext,
                _ => None
            };
            return this with
            {
                //set indexInContext to current index so the next track will be the next one in the context if we have one
                State = new WaveeLoadingState(
                    IndexInContext: indexInContext,
                    TrackId: Some(item.Id),
                    FromQueue: true,
                    StartFrom: TimeSpan.Zero,
                    StartPaused: false)
                {
                    Stream = item.StreamFuture()
                },
                IsShuffling = IsShuffling,
                RepeatState = RepeatState,
                Queue = newQueue
            };
        }

        //check if we have a context
        if (Context.IsSome)
        {
            //check if we have a next track
            //if not we are at the end of the context and repeat is enabled so we should start from the beginning
            //if not, we are at the end of the context and repeat is disabled so we should just return the current state
            //with EndOfContext set to true
            var context = Context.ValueUnsafe();
            var index = State switch
            {
                WaveeLoadingState loadingState => loadingState.IndexInContext,
                WaveePlayingState playingState => playingState.IndexInContext,
                WaveePausedState pausedState => pausedState.IndexInContext,
                WaveeEndedState endedState => endedState.IndexInContext,
                _ => None
            };

            var rep = RepeatState;
            var theoreticalNextIndex =
                IsShuffling
                    ? context.ShuffleProvider.IfNone(new RandomShuffler())
                        .GetNextIndex(index.IfNone(0), context.FutureTracks.Count())
                    : index.IfNone(0) + 1;


            var nextTrack = GetNextTrack(context, rep, theoreticalNextIndex);

            var st = State;
            return this with
            {
                State = nextTrack.Match(
                    Some: track => new WaveeLoadingState(
                        IndexInContext: Some(theoreticalNextIndex),
                        TrackId: Some(track.Id),
                        FromQueue: false,
                        StartFrom: TimeSpan.Zero,
                        StartPaused: false)
                    {
                        Stream = track.StreamFuture()
                    },
                    None: () => st switch
                    {
                        WaveeLoadingState loadingState => loadingState.ToEndedState(),
                        WaveePlayingState playingState => playingState.ToEndedState(),
                        WaveePausedState pausedState => pausedState.ToEndedState(),
                        _ => st
                    }),
                IsShuffling = IsShuffling,
                RepeatState = RepeatState,
                Queue = Queue
            };
        }

        //no context, no queue, no repeat state, so just return the current state
        return this with
        {
            State = State switch
            {
                WaveeLoadingState loadingState => loadingState with
                {
                    StartFrom = TimeSpan.Zero,
                    StartPaused = false
                },
                _ => State
            }
        };
    }

    private static Option<FutureTrack> GetNextTrack(WaveeContext context, RepeatStateType rep, int theoreticalNextIndex)
    {
        //check if we have a next track
        //if not we are at the end of the context and repeat is enabled so we should start from the beginning
        //if not, we are at the end of the context and repeat is disabled so we should just return the current state

        var nextTrack = context.FutureTracks.Skip(theoreticalNextIndex).HeadOrNone();
        if (nextTrack.IsNone)
        {
            if (rep is RepeatStateType.RepeatContext)
            {
                //we are repeating the context, so lets start from the beginning
                nextTrack = context.FutureTracks.HeadOrNone();
            }
        }

        return nextTrack;
    }
}

public readonly struct RandomShuffler : IShuffleProvider
{
    private static Random random = new();

    public int GetNextIndex(int currentIndex, int maxIndex)
    {
        var nextIndex = random.Next(0, maxIndex);
        return nextIndex == currentIndex ? GetNextIndex(currentIndex, maxIndex) : nextIndex;
    }
}

public readonly record struct WaveeContext(
    Option<IShuffleProvider> ShuffleProvider,
    AudioId Context,
    string Name,
    IEnumerable<FutureTrack> FutureTracks);

public readonly record struct FutureTrack(AudioId Id, Func<Task<IAudioStream>> StreamFuture);