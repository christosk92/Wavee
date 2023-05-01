using System.Net.Sockets;
using Wavee.Spotify.Infrastructure.Traits;

namespace Wavee.Spotify.Infrastructure.Live;

/// <summary>
/// Live IO runtime
/// </summary>
internal readonly struct Runtime :
    HasCancel<Runtime>,
    HasTCP<Runtime>,
    HasHttp<Runtime>
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
        new Runtime(new RuntimeEnv(new CancellationTokenSource(), new TcpClient(), new HttpClient()));
    
    /// <summary>
    /// Create a new Runtime with a fresh cancellation token
    /// </summary>
    /// <remarks>Used by localCancel to create new cancellation context for its sub-environment</remarks>
    /// <returns>New runtime</returns>
    public Runtime LocalCancel =>
        new Runtime(new RuntimeEnv(new CancellationTokenSource(), Env.Tcp, Env.HttpClient));

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

    public Eff<Runtime, Traits.TcpIO> TcpEff
        => Eff<Runtime, Traits.TcpIO>(static rt => new TcpIO(rt.Env.Tcp));

    public Eff<Runtime, Traits.HttpIO> HttpEff
        => Eff<Runtime, Traits.HttpIO>(static rt => new HttpIO(rt.Env.HttpClient));
}

internal sealed class RuntimeEnv
{
    public readonly CancellationTokenSource Source;
    public readonly CancellationToken Token;
    public readonly TcpClient Tcp;
    public readonly HttpClient HttpClient;

    public RuntimeEnv(CancellationTokenSource source, CancellationToken token, TcpClient tcp, HttpClient httpClient)
    {
        Source = source;
        Token = token;
        Tcp = tcp;
        HttpClient = httpClient;
    }

    public RuntimeEnv(CancellationTokenSource source, TcpClient tcp, HttpClient httpClient) : this(source, source.Token,
        tcp, httpClient)
    {
    }
}