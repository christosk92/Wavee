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
            StartFrom: Option<TimeSpan>.None
        );
    }

    public async Task<WaveePlayerState> SkipNext(bool setPermanentEndIfNothing)
    {
        //TODO: Queue
        var nextIndex = this.TrackIndex.ValueUnsafe() + 1;
        var nextTrack = Context.ValueUnsafe().FutureTracks.ElementAtOrDefault(nextIndex);
        if (nextTrack is null)
        {
            return this with
            {
                PermanentEnd = setPermanentEndIfNothing,
            };
        }

        var nextTrackData = await nextTrack.Factory(CancellationToken.None);
        return this with
        {
            TrackId = nextTrackData.Id,
            TrackUid = nextTrack.TrackUid,
            TrackIndex = nextIndex,
            TrackDetails = nextTrackData,
            PermanentEnd = false
        };
    }
}