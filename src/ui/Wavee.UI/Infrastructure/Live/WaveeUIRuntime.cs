using LanguageExt;
using LanguageExt.Effects.Traits;
using System.Text;
using Wavee.Spotify.Configs;
using Wavee.UI.Infrastructure.Traits;

namespace Wavee.UI.Infrastructure.Live;

/// <summary>
/// Live IO runtime
/// </summary>
public readonly struct WaveeUIRuntime :
    HasCancel<WaveeUIRuntime>,
    HasFile<WaveeUIRuntime>,
    HasEncoding<WaveeUIRuntime>,
    HasDirectory<WaveeUIRuntime>,
    HasTime<WaveeUIRuntime>,
    HasSpotify<WaveeUIRuntime>,
    HasLocalPath<WaveeUIRuntime>,
    HasEnvironment<WaveeUIRuntime>
{
    readonly RuntimeEnv env;

    /// <summary>
    /// Constructor
    /// </summary>
    WaveeUIRuntime(RuntimeEnv env) =>
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
    public static WaveeUIRuntime New(string localPath, SpotifyConfig config) =>
        new WaveeUIRuntime(new RuntimeEnv(new CancellationTokenSource(), System.Text.Encoding.Default, new LiveSpotify(config),
            localPath));


    /// <summary>
    /// Create a new Runtime with a fresh cancellation token
    /// </summary>
    /// <remarks>Used by localCancel to create new cancellation context for its sub-environment</remarks>
    /// <returns>New runtime</returns>
    public WaveeUIRuntime LocalCancel =>
        new WaveeUIRuntime(
            new RuntimeEnv(new CancellationTokenSource(), Env.Encoding, Env.SpotifyIO, Env.LocalDataPath));

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


    public WaveeUIRuntime WithPath(string homeValue) =>
        new WaveeUIRuntime(env with
        {
            LocalDataPath = homeValue
        });

    /// <summary>
    /// Get encoding
    /// </summary>
    /// <returns></returns>
    public Encoding Encoding =>
        Env.Encoding;

    public string Path => Env.LocalDataPath;

    /// <summary>
    /// Access the file environment
    /// </summary>
    /// <returns>File environment</returns>
    public Eff<WaveeUIRuntime, Traits.FileIO> FileEff =>
        SuccessEff(Live.FileIO.Default);

    /// <summary>
    /// Access the directory environment
    /// </summary>
    /// <returns>Directory environment</returns>
    public Eff<WaveeUIRuntime, Traits.DirectoryIO> DirectoryEff =>
        SuccessEff(Live.DirectoryIO.Default);

    /// <summary>
    /// Access the time environment
    /// </summary>
    /// <returns>Time environment</returns>
    public Eff<WaveeUIRuntime, Traits.TimeIO> TimeEff =>
        SuccessEff(Live.TimeIO.Default);

    public Eff<WaveeUIRuntime, Traits.SpotifyIO> SpotifyEff =>
        Eff<WaveeUIRuntime, Traits.SpotifyIO>(static rt => rt.Env.SpotifyIO);


    /// <summary>
    /// Access the operating-system environment
    /// </summary>
    /// <returns>Operating-system environment environment</returns>
    public Eff<WaveeUIRuntime, Traits.EnvironmentIO> EnvironmentEff =>
        SuccessEff(Live.EnvironmentIO.Default);

    public record RuntimeEnv
    {
        public CancellationTokenSource Source { get; init; }
        public CancellationToken Token { get; init; }
        public Encoding Encoding { get; init; }
        public Traits.SpotifyIO SpotifyIO { get; init; }
        public string LocalDataPath { get; init; }

        public RuntimeEnv(CancellationTokenSource source, CancellationToken token, Encoding encoding,
            Traits.SpotifyIO spotifyIo, string localDataPath)
        {
            Source = source;
            Token = token;
            Encoding = encoding;
            SpotifyIO = spotifyIo;
            LocalDataPath = localDataPath;
        }

        public RuntimeEnv(CancellationTokenSource source, Encoding encoding, Traits.SpotifyIO spotifyIo,
            string localDataPath) : this(source, source.Token, encoding, spotifyIo, localDataPath)
        {
        }
    }
}