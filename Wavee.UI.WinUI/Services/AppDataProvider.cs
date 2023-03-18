using System;
using Windows.Storage;
using Wavee.UI.Interfaces.Services;
using Wavee.UI.WinUI.Helpers.Runtime;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Wavee.UI.WinUI.Services
{
    internal sealed class AppDataProvider : IAppDataProvider
    {
        public string GetAppDataRoot()
        {
            if (RuntimeHelper.IsMSIX)
            {
                return ApplicationData.Current.LocalFolder.Path;
            }

            return GetDataDir("waveeui");
        }

        // appName, dataDir
        private static ConcurrentDictionary<string, string> DataDirDict { get; } =
            new ConcurrentDictionary<string, string>();

        // Do not change the output of this function. Backwards compatibility depends on it.
        private static string GetDataDir(string appName)
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
                    Debug.WriteLine($"Using HOME environment variable for initializing application data at `{directory}`.");
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
                    Debug.WriteLine(
                        $"Using APPDATA environment variable for initializing application data at `{directory}`.");
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

            Debug.WriteLine($"Creating data directory at `{directory}`.");
            Directory.CreateDirectory(directory);

            DataDirDict.TryAdd(appName, directory);
            return directory;
        }


        private static string GetFullBaseDirectory()
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
    }
}
