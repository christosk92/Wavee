using System.Reactive.Linq;
using System.Reactive.Subjects;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Wavee.Player;

public sealed class WaveePlayer
{
    private readonly Subject<Option<TimeSpan>> _positionUpdates = new();
    private static WaveePlayer _instance;
    private readonly Ref<WaveePlayerState> _state = Ref(WaveePlayerState.Empty());
    public static WaveePlayer Instance => _instance ??= new WaveePlayer();

    public async Task Play(IWaveeContext context, Option<int> indexInContext)
    {
        var track = context.ElementAtOrDefault(indexInContext.IfNone(0));
        if (track is null)
        {
            return;
        }

        var trackStream = await track.Factory();

        atomic(() => _state.Swap(x => x with
        {
            TrackId = track.TrackId,
            TrackUid = track.TrackUid,
            Context = Some(context),
            IsPaused = false, 
            IsShuffling = false,
            RepeatState = x.RepeatState switch
            {
                RepeatState.Context => RepeatState.Context,
                RepeatState.None => RepeatState.None,
                RepeatState.Track => RepeatState.Context
            },
            TrackDetails = trackStream
        }));
    }


    public IObservable<WaveePlayerState> StateUpdates => _state.OnChange().StartWith(_state.Value);
    public IObservable<Option<TimeSpan>> PositionUpdates => _positionUpdates.StartWith(Position);

    public WaveePlayerState State => _state.Value;
    public Option<TimeSpan> Position { get; }
}