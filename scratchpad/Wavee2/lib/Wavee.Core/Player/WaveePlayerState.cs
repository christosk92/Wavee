using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Playback;
using Wavee.Core.Player.InternalCommanding;
using Wavee.Core.Player.PlaybackStates;

namespace Wavee.Core.Player;

public readonly record struct WaveePlayerState(
    IWaveePlaybackState PlaybackState,
    RepeatState RepeatState,
    bool IsShuffling,
    Option<WaveeContext> Context,
    Que<FutureTrack> ManualQueue)
{
    public static WaveePlayerState Default = new WaveePlayerState(
        PlaybackState: NonePlaybackState.Default,
        RepeatState.None,
        IsShuffling: false,
        Context: Option<WaveeContext>.None,
        ManualQueue: Que<FutureTrack>.Empty
    );

    public WaveePlayerState SkipNext(bool ignoreRepeatState)
    {
        //Couple of things to note here:
        //IgnoreRepeatState overrides RepeatState.Track (NOT CONTEXT!!!) AND Queue
        //repeat state beats shuffle
        if (ignoreRepeatState && RepeatState is RepeatState.Track)
        {
            //call ourselves again with ignoreRepeatState = false and RepeatState = Context
            return (this with
            {
                RepeatState = RepeatState.Context
            }).SkipNext(false);
        }

        //check for queue
        if (ManualQueue.Length > 0)
        {
            //TODO:
            return this;
        }

        //check for context
        if (Context.IsSome)
        {
            var context = Context.ValueUnsafe();
            var currentIndex = PlaybackState switch
            {
                WaveePlaybackPlayingState playing => playing.IndexInContext,
                WaveePlaybackLoadingState loading => loading.IndexInContext,
                WaveePlaybackEndedState endedState => endedState.IndexInContext,
                _ => 0
            };

            var theorticalNextIndex = currentIndex + 1;
            FutureTrack nextTrack = default;
            try
            {
                nextTrack = context.FutureTracks.ElementAt(theorticalNextIndex);
            }
            catch (ArgumentOutOfRangeException x)
            {
                //check if we're repeating the context
                if (RepeatState is RepeatState.Context)
                {
                    try
                    {
                        nextTrack = context.FutureTracks.ElementAt(0);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        //permanent end of context
                        return this with
                        {
                            PlaybackState = PermanentEndOfContextPlaybackState.Default,
                            Context = context
                        };
                    }
                }

                //permanent end of context
                return this with
                {
                    PlaybackState = PermanentEndOfContextPlaybackState.Default,
                    Context = context
                };
            }

            var playbackState = new WaveePlaybackLoadingState(
                Stream: nextTrack.StreamFuture(),
                TrackId: nextTrack.Id,
                IndexInContext: theorticalNextIndex,
                FromQueue: false,
                StartAt: Option<TimeSpan>.None,
                StartPaused: false
            );

            return this with
            {
                PlaybackState = playbackState,
                Context = context
            };
        }

        //permanent end of context
        return this with
        {
            PlaybackState = PermanentEndOfContextPlaybackState.Default,
            Context = Option<WaveeContext>.None
        };
    }

    public WaveePlayerState FromNewContext(WaveeContext context, Option<int> startFromIndexInContext)
    {
        try
        {
            var realisticIndex = startFromIndexInContext.IfNone(0);
            var trackAtStart = context.FutureTracks.ElementAt(realisticIndex);
            var playbackState = new WaveePlaybackLoadingState(
                Stream: trackAtStart.StreamFuture(),
                TrackId: trackAtStart.Id,
                IndexInContext:
                realisticIndex,
                FromQueue: false,
                StartAt: Option<TimeSpan>.None,
                StartPaused: false
            );

            return this with
            {
                PlaybackState = playbackState,
                Context = context
            };
        }
        catch (ArgumentOutOfRangeException)
        {
            //permanent end of context
            return this with
            {
                PlaybackState = PermanentEndOfContextPlaybackState.Default,
                Context = context
            };
        }
    }

    public WaveePlayerState FromPlayingTrack(WaveePlaybackLoadingState loadingState,
        IAudioDecoder decoder,
        IAudioStream stream)
    {
        return this with
        {
            PlaybackState = new WaveePlaybackPlayingState(
                Decoder: decoder,
                Stream: stream,
                TrackId: loadingState.TrackId,
                IndexInContext: loadingState.IndexInContext,
                FromQueue: loadingState.FromQueue,
                Paused: false
            )
        };
    }

    public WaveePlayerState FromEndedTrack(bool crossfading)
    {
        return this with
        {
            PlaybackState = this.PlaybackState switch
            {
                WaveePlaybackPlayingState playing => new WaveePlaybackEndedState(
                    Decoder: playing.Decoder,
                    Stream: playing.Stream,
                    IsPlaying: playing.IsPlaying,
                    TrackId: playing.TrackId,
                    CrossfadingIntoNextTrack: crossfading,
                    IndexInContext: playing.IndexInContext
                ),
                _ => this.PlaybackState
            }
        };
    }
}