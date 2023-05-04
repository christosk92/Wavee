using LanguageExt.Effects.Traits;
using Wavee.Infrastructure.Traits;

namespace Wavee.Infrastructure.Live;

public readonly struct WaveeRuntime :
    HasCancel<WaveeRuntime>,
    HasTCP<WaveeRuntime>,
    HasHttp<WaveeRuntime>
{
    readonly RuntimeEnv env;

    /// <summary>
    /// Constructor
    /// </summary>
    WaveeRuntime(RuntimeEnv env) =>
        this.env = env;

    /// <summary>
    /// Configuration environment accessor
    /// </summary>
    internal RuntimeEnv Env =>
        env ?? throw new InvalidOperationException(
            "Runtime Env not set.  Perhaps because of using default(Runtime) or new Runtime() rather than Runtime.New()");

    /// <summary>
    /// Constructor function
    /// </summary>
    public static WaveeRuntime New() =>
        new WaveeRuntime(new RuntimeEnv(new CancellationTokenSource(), new NAudioHolder()));

    /// <summary>
    /// Create a new Runtime with a fresh cancellation token
    /// </summary>
    /// <remarks>Used by localCancel to create new cancellation context for its sub-environment</remarks>
    /// <returns>New runtime</returns>
    public WaveeRuntime LocalCancel =>
        new WaveeRuntime(new RuntimeEnv(new CancellationTokenSource(), new NAudioHolder()));

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

    public Eff<WaveeRuntime, Traits.TcpIO> TcpEff =>
        SuccessEff(Live.TcpIOImpl.Default);

    public Eff<WaveeRuntime, HttpIO> HttpEff =>
        SuccessEff(Live.HttpIOImpl.Default);

    
    public Eff<WaveeRuntime, Traits.AudioOutputIO> AudioOutputEff
        => Eff<WaveeRuntime, Traits.AudioOutputIO>(static rt => new AudioOutputIO(rt.Env.NAudioHolder));

    
    internal class RuntimeEnv
    {
        public readonly CancellationTokenSource Source;
        public readonly CancellationToken Token;
        public readonly NAudioHolder NAudioHolder;

        public RuntimeEnv(CancellationTokenSource source, CancellationToken token, NAudioHolder nAudioHolder)
        {
            Source = source;
            Token = token;
            NAudioHolder = nAudioHolder;
        }

        public RuntimeEnv(CancellationTokenSource source, NAudioHolder nAudioHolder) : this(source, source.Token, nAudioHolder)
        {
        }
    }
}