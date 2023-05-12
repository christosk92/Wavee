using System.Runtime.InteropServices;

namespace Wavee.UI.Helpers;

public static class StartupHelper
{
    public const string SilentArgument = "startsilent";

    public static Task ModifyStartupSettingAsync(bool runOnSystemStartup)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            WindowsStartupHelper.AddOrRemoveRegistryKey(runOnSystemStartup);
            return Task.CompletedTask;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // await LinuxStartupHelper.AddOrRemoveDesktopFileAsync(runOnSystemStartup).ConfigureAwait(false);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // await MacOsStartupHelper.AddOrRemoveLoginItemAsync(runOnSystemStartup).ConfigureAwait(false);
        }
        throw new NotSupportedException("This platform is not supported.");
    }
}