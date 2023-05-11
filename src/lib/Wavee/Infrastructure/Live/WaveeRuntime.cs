using LanguageExt.Effects.Traits;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Wavee.Infrastructure.Traits;

namespace Wavee.Infrastructure.Live;

public readonly struct WaveeRuntime :
    HasCancel<WaveeRuntime>,
    HasTCP<WaveeRuntime>,
    HasHttp<WaveeRuntime>,
    HasAudioOutput<WaveeRuntime>,
    HasWebsocket<WaveeRuntime>,
    HasLog<WaveeRuntime>
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
    public static WaveeRuntime New(Option<ILogger> logger) =>
        new WaveeRuntime(new RuntimeEnv(new CancellationTokenSource(), 
            logger.IfNone(NullLogger.Instance), new VlcHolder()));

    /// <summary>
    /// Create a new Runtime with a fresh cancellation token
    /// </summary>
    /// <remarks>Used by localCancel to create new cancellation context for its sub-environment</remarks>
    /// <returns>New runtime</returns>
    public WaveeRuntime LocalCancel =>
        new WaveeRuntime(new RuntimeEnv(new CancellationTokenSource(),  Env.Logger, new VlcHolder()));

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


    public Eff<WaveeRuntime, Traits.LogIO> LogEff => Eff<WaveeRuntime, Traits.LogIO>(static
        rt => new LogIO(rt.env.Logger));

    public Eff<WaveeRuntime, Traits.TcpIO> TcpEff =>
        SuccessEff(Live.TcpIOImpl.Default);

    public Eff<WaveeRuntime, HttpIO> HttpEff =>
        SuccessEff(Live.HttpIOImpl.Default);


    public Eff<WaveeRuntime, Traits.AudioOutputIO> AudioOutputEff
        => Eff<WaveeRuntime, Traits.AudioOutputIO>(static rt => new VlcAudioOutputIO(rt.Env.VlcHolder));

    public Eff<WaveeRuntime, WebsocketIO> WsEff =>
        SuccessEff(Live.WebsocketIOImpl.Default);

    internal class RuntimeEnv
    {
        public readonly CancellationTokenSource Source;
        public readonly CancellationToken Token;
        public readonly VlcHolder VlcHolder;
        public ILogger Logger;

        public RuntimeEnv(CancellationTokenSource source, CancellationToken token, 
            ILogger logger, VlcHolder vlcHolder)
        {
            Source = source;
            Token = token;
            Logger = logger;
            VlcHolder = vlcHolder;
        }

        public RuntimeEnv(CancellationTokenSource source, ILogger logger, VlcHolder vlcHolder) : this(source,
            source.Token, logger, vlcHolder)
        {
        }
    }
}