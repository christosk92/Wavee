using System.Runtime.InteropServices;

namespace Wavee.Spotify.Helpers.Extensions;

internal static class OS
{
    public static OSPlatformType MatchPlatform()
    {
        return IsWindows()
            ? OSPlatformType.Windows
            : IsIos()
                ? OSPlatformType.OSX
                : OSPlatformType.Linux;
    }

    private static bool IsWindows()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    private static bool IsIos()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }

    private static bool IsAndroid()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
               || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);
    }
}

internal enum OSPlatformType
{
    Windows,
    OSX,
    Linux,
    Android,
    iOS
}