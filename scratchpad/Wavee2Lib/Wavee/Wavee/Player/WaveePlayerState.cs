using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Ids;

namespace Wavee.Player;

public readonly record struct WaveePlayerState(
    Option<AudioId> TrackId,
    Option<string> TrackUid,
    Option<int> TrackIndex,
    bool IsPaused,
    bool IsShuffling,
    RepeatState RepeatState,
    Option<WaveeContext> Context,
    Option<WaveeTrack> TrackDetails,
    Option<TimeSpan> StartFrom,
    Que<FutureWaveeTrack> Queue,
    bool PermanentEnd = false)
{
    public static WaveePlayerState Empty()
    {
        return new WaveePlayerState(
            TrackId: Option<AudioId>.None,
            TrackUid: Option<string>.None,
            TrackIndex: Option<int>.None,
            IsPaused: false,
            IsShuffling: false,
            RepeatState: RepeatState.None,
            Context: Option<WaveeContext>.None,
            TrackDetails: Option<WaveeTrack>.None,
            StartFrom: Option<TimeSpan>.None,
            Queue: Que<FutureWaveeTrack>.Empty
        );
    }

    public async Task<WaveePlayerState> SkipNext(bool setPermanentEndIfNothing, bool overrideRepeatState = false)
    {
        //repeat state (=track) beats queue
        if (!overrideRepeatState && RepeatState is RepeatState.Track)
        {
            //return US
            return this with
            {
                PermanentEnd = false,
                StartFrom = Option<TimeSpan>.None
            };
        }

        //queue!
        if (!this.Queue.IsEmpty)
        {
            var queuedTrack = this.Queue.Peek();
            var queuedTrackData = await queuedTrack.Factory(CancellationToken.None);
            return this with
            {
                TrackId = queuedTrackData.Id,
                TrackUid = queuedTrack.TrackUid,
                TrackIndex = this.TrackIndex, //keep track index for potential next track
                TrackDetails = queuedTrackData,
                PermanentEnd = false,
                StartFrom = Option<TimeSpan>.None,
                Queue = this.Queue.Dequeue()
            };
        }

        var ctx = Context.ValueUnsafe();
        var currentIndex = this.TrackIndex.IfNone(0);
        int nextIndex = 0;
        FutureWaveeTrack? nextTrack = null;
        if (IsShuffling)
        {
            nextIndex = ctx.ShuffleProvider.GetNextIndex(currentIndex);
            nextTrack = ctx.FutureTracks.ElementAtOrDefault(nextIndex);
        }
        else
        {
            var theoreticalNext = currentIndex + 1;
            nextTrack = ctx.FutureTracks.ElementAtOrDefault(theoreticalNext);
            if (nextTrack is null && RepeatState is RepeatState.Context)
            {
                theoreticalNext = 0;
                nextTrack = ctx.FutureTracks.ElementAtOrDefault(0);
            }
            nextIndex = theoreticalNext;
        }

        if (nextTrack is null)
        {
            return this with
            {
                PermanentEnd = setPermanentEndIfNothing,
                StartFrom = new Option<TimeSpan>(),
                RepeatState = this.RepeatState switch
                {
                    RepeatState.Track => RepeatState.Context,
                    RepeatState.Context => RepeatState.Context,
                    RepeatState.None => RepeatState.None
                }
            };
        }

        var nextTrackData = await nextTrack.Factory(CancellationToken.None);
        return this with
        {
            TrackId = nextTrackData.Id,
            TrackUid = nextTrack.TrackUid,
            TrackIndex = nextIndex,
            TrackDetails = nextTrackData,
            PermanentEnd = false,
            StartFrom = Option<TimeSpan>.None,
            RepeatState = this.RepeatState switch
            {
                RepeatState.Track => RepeatState.Context,
                RepeatState.Context => RepeatState.Context,
                RepeatState.None => RepeatState.None
            }
        };
    }
}