using System.Reactive.Linq;
using LanguageExt;
using Wavee.Player.State;
using static LanguageExt.Prelude;
namespace Wavee.Player;

public sealed class WaveePlayer : IWaveePlayer
{
    private readonly Ref<Option<WaveePlayerState>> _state = Ref(Option<WaveePlayerState>.None);
    public IObservable<Option<WaveePlayerState>> CreateListener() => _state.OnChange().StartWith(_state);
}

public interface IWaveePlayer
{
    IObservable<Option<WaveePlayerState>> CreateListener();
}