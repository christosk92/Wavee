using Wavee.Context;
using Wavee.Infrastructure.Live;

namespace Wavee;

public sealed class WaveePlayer : IWaveePlayer
{
    private readonly Ref<IWaveeState> _state = Ref<IWaveeState>(new States.InvalidState());
    private readonly Runtime _runtime;

    public WaveePlayer()
    {
        _runtime = Audio.Runtime;
    }

    public async ValueTask<Unit> Play(IWaveeContext context, Option<int> startAt, TimeSpan startAtTime, bool play)
    {
        var trackId = await context.GetIdAt(startAt);
        
        return unit;
    }

    public Option<TimeSpan> Position { get; }
    public IObservable<IWaveeState> StateChanged => _state.OnChange();
}

public interface IWaveePlayer
{
    ValueTask<Unit> Play(IWaveeContext context, Option<int> startAt, TimeSpan startAtTime, bool play);
    Option<TimeSpan> Position { get; }
    IObservable<IWaveeState> StateChanged { get; }
}