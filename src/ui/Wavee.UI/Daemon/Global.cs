namespace Wavee.UI.Daemon;

public class Global
{
    /// <remarks>Use this variable as a guard to prevent touching <see cref="StoppingCts"/> that might have already been disposed.</remarks>
    private volatile bool _disposeRequested;

    public Global(string dataDir, Config config)
    {
        DataDir = dataDir;
        Config = config;
    }

    public string DataDir { get; }
    public Config Config { get; }
}