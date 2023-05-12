using WalletWasabi.Helpers;
using Wavee.UI.Daemon;

namespace Wavee.UI;

public static class Services
{
    public static string DataDir { get; private set; } = null!;

    public static PersistentConfig PersistentConfig { get; private set; } = null!;

    public static UiConfig UiConfig { get; private set; } = null!;

    public static bool IsInitialized { get; private set; }


    /// <summary>
    /// Initializes global services used by fluent project.
    /// </summary>
    /// <param name="global">The global instance.</param>
    public static void Initialize(Global global, UiConfig uiConfig)
    {
        Guard.NotNull(nameof(global.DataDir), global.DataDir);
        Guard.NotNull(nameof(global.Config), global.Config);
        Guard.NotNull(nameof(uiConfig), uiConfig);

        DataDir = global.DataDir;
        PersistentConfig = global.Config.PersistentConfig;
        UiConfig = uiConfig;

        IsInitialized = true;
    }
}