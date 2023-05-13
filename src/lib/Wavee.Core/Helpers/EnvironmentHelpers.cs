using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Wavee.Core.Helpers;

internal static class EnvironmentHelpers
{
    [Flags]
    private enum EXECUTION_STATE : uint
    {
        ES_AWAYMODE_REQUIRED = 0x00000040,
        ES_CONTINUOUS = 0x80000000,
        ES_DISPLAY_REQUIRED = 0x00000002,
        ES_SYSTEM_REQUIRED = 0x00000001
    }

    // appName, dataDir
    private static ConcurrentDictionary<string, string> DataDirDict { get; } =
        new ConcurrentDictionary<string, string>();

    // Do not change the output of this function. Backwards compatibility depends on it.
    public static string GetDataDir(string appName)
    {
        if (DataDirDict.TryGetValue(appName, out string? dataDir))
        {
            return dataDir;
        }

        string directory;

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
            {
                directory = Path.Combine(home, "." + appName.ToLowerInvariant());
            }
            else
            {
                throw new DirectoryNotFoundException("Could not find suitable datadir.");
            }
        }
        else
        {
            var localAppData = Environment.GetEnvironmentVariable("APPDATA");
            if (!string.IsNullOrEmpty(localAppData))
            {
                directory = Path.Combine(localAppData, appName);
            }
            else
            {
                throw new DirectoryNotFoundException("Could not find suitable datadir.");
            }
        }

        if (Directory.Exists(directory))
        {
            DataDirDict.TryAdd(appName, directory);
            return directory;
        }

        Directory.CreateDirectory(directory);

        DataDirDict.TryAdd(appName, directory);
        return directory;
    }

    /// <summary>
    /// Gets medialoc delivery <c>datadir</c> parameter from:
    /// <list type="bullet">
    /// <item><c>APPDATA</c> environment variable on Windows, and</item>
    /// <item><c>HOME</c> environment variable on other platforms.</item>
    /// </list>
    /// </summary>
    /// <returns><c>datadir</c> or empty string.</returns>
    public static string GetDefaultDeliveryCoreDataDirOrEmptyString()
    {
        string directory = "";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var localAppData = Environment.GetEnvironmentVariable("APPDATA");
            if (!string.IsNullOrEmpty(localAppData))
            {
                directory = Path.Combine(localAppData, "Medialoc Delivery");
            }
        }
        else
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
            {
                directory = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    ? Path.Combine(home, "Library", "Application Support", "Medialoc Delivery")
                    : Path.Combine(home, ".medialocdelivery"); // Linux
            }
        }

        return directory;
    }

    // This method removes the path and file extension.
    //
    // Given the releases are currently built using Windows, the generated assemblies contain
    // the hardcoded "C:\Users\User\Desktop\Delivery\.......\FileName.cs" string because that
    // is the real path of the file, it doesn't matter what OS was targeted.
    // In Windows and Linux that string is a valid path and that means Path.GetFileNameWithoutExtension
    // can extract the file name but in the case of OSX the same string is not a valid path so, it assumes
    // the whole string is the file name.
    public static string ExtractFileName(string callerFilePath)
    {
        var lastSeparatorIndex = callerFilePath.LastIndexOf("\\");
        if (lastSeparatorIndex == -1)
        {
            lastSeparatorIndex = callerFilePath.LastIndexOf("/");
        }

        var fileName = callerFilePath;

        if (lastSeparatorIndex != -1)
        {
            lastSeparatorIndex++;
            fileName = callerFilePath[lastSeparatorIndex..]; // From lastSeparatorIndex until the end of the string.
        }

        var fileNameWithoutExtension = fileName.TrimEnd(".cs", StringComparison.InvariantCultureIgnoreCase);
        return fileNameWithoutExtension;
    }
    
    public static bool IsFileTypeAssociated(string fileExtension)
    {
        // Source article: https://edi.wang/post/2019/3/4/read-and-write-windows-registry-in-net-core

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new InvalidOperationException("Operation only supported on windows.");
        }

        fileExtension = fileExtension.TrimStart('.'); // Remove . if added by the caller.

        using (var key = Registry.ClassesRoot.OpenSubKey($".{fileExtension}"))
        {
            // Read the (Default) value.
            if (key?.GetValue(null) is not null)
            {
                return true;
            }
        }

        return false;
    }

    public static string GetFullBaseDirectory()
    {
        var fullBaseDirectory = Path.GetFullPath(AppContext.BaseDirectory);

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (!fullBaseDirectory.StartsWith('/'))
            {
                fullBaseDirectory = fullBaseDirectory.Insert(0, "/");
            }
        }

        return fullBaseDirectory;
    }

    public static string GetExecutablePath()
    {
        var fullBaseDir = GetFullBaseDirectory();
        var deliveryFileName = Path.Combine(fullBaseDir, Consts.ExecutableName);
        deliveryFileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? $"{deliveryFileName}.exe"
            : $"{deliveryFileName}";
        if (File.Exists(deliveryFileName))
        {
            return deliveryFileName;
        }

        var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ??
                           throw new NullReferenceException("Assembly or Assembly's Name was null.");
        var fluentExecutable = Path.Combine(fullBaseDir, assemblyName);
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"{fluentExecutable}.exe" : $"{fluentExecutable}";
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);
}

public static class Consts
{
    public static readonly Version ClientVersion = new(1, 1, 0, 1);

    public const string ExecutableName = "Medialoc.Delivery.UI";

    public const string AppName = "Medialoc Delivery";
}