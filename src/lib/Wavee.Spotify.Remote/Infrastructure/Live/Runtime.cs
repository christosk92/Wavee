using System.Net.WebSockets;
using Wavee.Spotify.Infrastructure.Traits;
using Wavee.Spotify.Remote.Infrastructure.Traits;

namespace Wavee.Spotify.Remote.Infrastructure.Live;

internal readonly struct Runtime :
    HasCancel<Runtime>,
    HasWs<Runtime>,
    Wavee.Spotify.Infrastructure.Traits.HasHttp<Runtime>,
    HasTime<Runtime>
{
    readonly RuntimeEnv env;

    /// <summary>
    /// Constructor
    /// </summary>
    Runtime(RuntimeEnv env) =>
        this.env = env;

    /// <summary>
    /// Configuration environment accessor
    /// </summary>
    public RuntimeEnv Env =>
        env ?? throw new InvalidOperationException(
            "Runtime Env not set.  Perhaps because of using default(Runtime) or new Runtime() rather than Runtime.New()");

    /// <summary>
    /// Constructor function
    /// </summary>
    public static Runtime New() =>
        new Runtime(new RuntimeEnv(new CancellationTokenSource(), new ClientWebSocket
        {
            Options =
            {
                KeepAliveInterval = TimeSpan.FromDays(7)
            }
        }, new HttpClient()));

    /// <summary>
    /// Create a new Runtime with a fresh cancellation token
    /// </summary>
    /// <remarks>Used by localCancel to create new cancellation context for its sub-environment</remarks>
    /// <returns>New runtime</returns>
    public Runtime LocalCancel =>
        new Runtime(new RuntimeEnv(new CancellationTokenSource(), Env.Ws, Env.HttpClient));

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

    public Eff<Runtime, Traits.WsIO> WsEff
        => Eff<Runtime, Traits.WsIO>(static rt => new WsIO(rt.Env.Ws));

    public Eff<Runtime, Wavee.Spotify.Infrastructure.Traits.HttpIO> HttpEff
        => Eff<Runtime, Wavee.Spotify.Infrastructure.Traits.HttpIO>(static rt => new Wavee.Spotify.Infrastructure.Live.HttpIO(rt.Env.HttpClient));

    /// <summary>
    /// Access the time environment
    /// </summary>
    /// <returns>Time environment</returns>
    public Eff<Runtime, Traits.TimeIO> TimeEff =>
        SuccessEff(Live.TimeIO.Default);
}

internal sealed class RuntimeEnv
{
    public readonly CancellationTokenSource Source;
    public readonly CancellationToken Token;
    public readonly ClientWebSocket Ws;
    public readonly HttpClient HttpClient;

    public RuntimeEnv(CancellationTokenSource source, CancellationToken token, ClientWebSocket ws,
        HttpClient httpClient)
    {
        Source = source;
        Token = token;
        Ws = ws;
        HttpClient = httpClient;
    }

    public RuntimeEnv(CancellationTokenSource source, ClientWebSocket ws, HttpClient httpClient) : this(source,
        source.Token,
        ws, httpClient)
    {
    }
}