using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Wavee.Core.Infrastructure.Traits;

namespace Wavee.Core.Infrastructure.Live;

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
    public static WaveeRuntime New(Option<ILogger> logger, Option<AudioOutputIO> audioOutput) =>
        new WaveeRuntime(new RuntimeEnv(new CancellationTokenSource(),
            logger.IfNone(NullLogger.Instance), audioOutput));

    /// <summary>
    /// Create a new Runtime with a fresh cancellation token
    /// </summary>
    /// <remarks>Used by localCancel to create new cancellation context for its sub-environment</remarks>
    /// <returns>New runtime</returns>
    public WaveeRuntime LocalCancel =>
        new WaveeRuntime(new RuntimeEnv(new CancellationTokenSource(), Env.Logger, Env.AudioOutputIo));

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

    public Eff<WaveeRuntime, TcpIO> TcpEff =>
        SuccessEff(Live.TcpIOImpl.Default);

    public Eff<WaveeRuntime, HttpIO> HttpEff =>
        SuccessEff(Live.HttpIOImpl.Default);


    public Eff<WaveeRuntime, Option<AudioOutputIO>> AudioOutputEff
        => Eff<WaveeRuntime, Option<AudioOutputIO>>(static rt => rt.Env.AudioOutputIo);

    public Eff<WaveeRuntime, WebsocketIO> WsEff =>
        SuccessEff(Live.WebsocketIOImpl.Default);
    
    internal class RuntimeEnv
    {
        public readonly CancellationTokenSource Source;
        public readonly CancellationToken Token;
        public Option<AudioOutputIO> AudioOutputIo;
        public ILogger Logger;

        public RuntimeEnv(CancellationTokenSource source, CancellationToken token,
            ILogger logger, Option<AudioOutputIO> audioOutputIo)
        {
            Source = source;
            Token = token;
            Logger = logger;
            AudioOutputIo = audioOutputIo;
        }

        public RuntimeEnv(CancellationTokenSource source, ILogger logger, Option<AudioOutputIO> audioOutputIo) :
            this(source,
                source.Token, logger, audioOutputIo)
        {
        }
    }
}