using Wavee.Infrastructure.Traits;

namespace Wavee.Infrastructure.Live;

internal readonly struct Runtime
    : HasAudio<Runtime>
{
    readonly RuntimeEnv env;

    /// <summary>
    /// Constructor
    /// </summary>
    Runtime(RuntimeEnv env) =>
        this.env = env;

    /// <summary>
    /// Constructor function
    /// </summary>
    public static Runtime New() =>
        new Runtime(new RuntimeEnv(new CancellationTokenSource(), new NAudioHolder()));

    /// <summary>
    /// Configuration environment accessor
    /// </summary>
    public RuntimeEnv Env =>
        env ?? throw new InvalidOperationException(
            "Runtime Env not set.  Perhaps because of using default(Runtime) or new Runtime() rather than Runtime.New()");

    /// <summary>
    /// Create a new Runtime with a fresh cancellation token
    /// </summary>
    /// <remarks>Used by localCancel to create new cancellation context for its sub-environment</remarks>
    /// <returns>New runtime</returns>
    public Runtime LocalCancel =>
        new Runtime(new RuntimeEnv(new CancellationTokenSource(), Env.NAudioHolder));

    /// <summary>
    /// Direct access to cancellation token
    /// </summary>
    public CancellationToken CancellationToken =>
        Env.Token;

    /// <summary>
    /// Directly access the cancellation token source
    /// </summary>
    /// <returns>CancellationTokenSource</returns>
    public CancellationTokenSource CancellationTokenSource =>
        Env.Source;

    public Eff<Runtime, Traits.AudioIO> AudioEff
        => Eff<Runtime, Traits.AudioIO>(static rt => new AudioIO(rt.Env.NAudioHolder));
}

internal sealed class RuntimeEnv
{
    public readonly CancellationTokenSource Source;
    public readonly CancellationToken Token;
    public readonly NAudioHolder NAudioHolder;

    public RuntimeEnv(CancellationTokenSource source, CancellationToken token, NAudioHolder nAudioHolder)
    {
        Source = source;
        NAudioHolder = nAudioHolder;
    }

    public RuntimeEnv(CancellationTokenSource source, NAudioHolder nAudioHolder) : this(source,
        source.Token, nAudioHolder)
    {
    }
}